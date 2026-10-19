using System.Windows;
using System.Windows.Threading;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using SatWatch.App.Models;
using SatWatch.App.Services;
using SatWatch.App.Views;

namespace SatWatch.App;

public partial class MainWindow : Window
{
    private AppConfig _cfg = ConfigService.Load();
    private readonly List<SatelliteTrack> _tracks = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    public MainWindow()
    {
        InitializeComponent();
        if (MaidenheadConverter.TryParse(_cfg.Maidenhead, out var lat, out var lon))
        {
            _cfg.Latitude = lat; _cfg.Longitude = lon;
        }
        Loaded += (_, _) => InitMap();
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    private void InitMap()
    {
        Map.MapProvider = GMapProviders.ArcGIS_Imagery_World_2D_Map;
        Map.Position = new PointLatLng(_cfg.Latitude, _cfg.Longitude);
        Map.MinZoom = 2; Map.MaxZoom = 18; Map.Zoom = 4;
        Map.CanDragMap = true;
        Map.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
        RebuildTracks();
        DrawOverlays();
    }

    private void RebuildTracks()
    {
        _tracks.Clear();
        foreach (var fav in _cfg.Favorites.Where(f => f.Enabled))
        {
            var t = TrackerService.Build(fav, _cfg.Latitude, _cfg.Longitude);
            if (t != null) _tracks.Add(t);
        }
        foreach (var t in _tracks)
            TrackerService.ComputePasses(t, DateTime.UtcNow, DateTime.UtcNow.AddDays(2));
    }

    private void DrawOverlays()
    {
        Map.Markers.Clear();
        // Home pin
        // Basestation icon
        var bsCanvas = new System.Windows.Controls.Canvas { Width = 24, Height = 24 };
        var tri = new System.Windows.Shapes.Polygon { Points = new System.Windows.Media.PointCollection { new System.Windows.Point(12,0), new System.Windows.Point(24,24), new System.Windows.Point(0,24) }, Fill = System.Windows.Media.Brushes.OrangeRed, Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 1 };
        bsCanvas.Children.Add(tri);
        var homeMarker = new GMapMarker(new PointLatLng(_cfg.Latitude, _cfg.Longitude))
        {
            Shape = bsCanvas,
            Offset = new Point(-12, -12)
        };
        Map.Markers.Add(homeMarker);

        foreach (var track in _tracks)
        {
            var satCanvas = new System.Windows.Controls.Canvas { Width = 16, Height = 16 };
            var satDot = new System.Windows.Shapes.Ellipse { Width = 10, Height = 10, Fill = System.Windows.Media.Brushes.Lime, Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 1 };
            System.Windows.Controls.Canvas.SetLeft(satDot, 3); System.Windows.Controls.Canvas.SetTop(satDot, 3);
            satCanvas.Children.Add(satDot);
            var satLabel = new System.Windows.Controls.TextBlock { Text = track.Favorite.Name, Foreground = System.Windows.Media.Brushes.Yellow, FontSize = 10, FontWeight = FontWeights.Bold };
            System.Windows.Controls.Canvas.SetLeft(satLabel, 16); System.Windows.Controls.Canvas.SetTop(satLabel, 0);
            satCanvas.Children.Add(satLabel);
            var satMarker = new GMapMarker(new PointLatLng(track.Lat, track.Lon))
            {
                Shape = satCanvas,
                Offset = new Point(-8, -8)
            };
            Map.Markers.Add(satMarker);

            // Footprint circle as polygon
            if (track.Footprint.Count > 2)
            {
                var poly = new GMapPolygon(track.Footprint.Select(p => new PointLatLng(p.lat, p.lon)).ToList())
                {
                    Shape = new System.Windows.Shapes.Polygon { Stroke = System.Windows.Media.Brushes.Cyan, StrokeThickness = 1, Fill = System.Windows.Media.Brushes.Transparent }
                };
                Map.Markers.Add(poly);
            }

            // Path line
            if (track.Path.Count > 1)
            {
                var route = new GMapRoute(track.Path.Select(p => new PointLatLng(p.lat, p.lon)).ToList())
                {
                    Shape = new System.Windows.Shapes.Polyline { Stroke = System.Windows.Media.Brushes.Lime, StrokeThickness = 2 }
                };
                Map.Markers.Add(route);
            }
        }
    }

    private void Tick()
    {
        foreach (var t in _tracks)
        {
            TrackerService.UpdatePosition(t);
        }
        DrawOverlays();
        UpdatePassList();
        RunAlarms();
        StatusText.Text = $"Home: {_cfg.Maidenhead} | Sats: {_tracks.Count} | {DateTime.Now:T}";
        // TLE auto update check
        if (_cfg.AutoUpdateTle && (DateTime.UtcNow - _cfg.LastTleUpdateUtc).TotalDays >= _cfg.TleUpdateDays)
        {
            _ = AutoUpdateTleAsync();
        }
    }

    private async Task AutoUpdateTleAsync()
    {
        try
        {
            _cfg.LastTleUpdateUtc = DateTime.UtcNow;
            var raw = await TleService.FetchRawAsync(_cfg.TleUpdateUrl);
            var fetched = TleService.ParseTleText(raw);
            TleService.UpdateFavorites(_cfg, fetched);
            ConfigService.Save(_cfg);
            RebuildTracks();
        }
        catch { }
    }

    private void UpdatePassList()
    {
        var items = new List<string>();
        foreach (var t in _tracks)
        {
            foreach (var p in t.Passes.Take(3))
            {
                items.Add($"{t.Favorite.Name}: AOS {p.Start.ToLocalTime():g} TCA {p.MaxElevationTime.ToLocalTime():t} LOS {p.End.ToLocalTime():t}");
            }
        }
        PassList.ItemsSource = items;
    }

    private void RunAlarms()
    {
        var now = DateTime.UtcNow;
        foreach (var rule in _cfg.Alarms.Where(a => a.Enabled))
        {
            var track = _tracks.FirstOrDefault(t => t.Favorite.Name == rule.SatelliteName);
            if (track == null || track.NextPass == null) continue;
            DateTime milestone = rule.Milestone switch
            {
                AlarmMilestone.AOS => track.NextPass.Start,
                AlarmMilestone.TCA => track.NextPass.MaxElevationTime,
                AlarmMilestone.LOS => track.NextPass.End,
                _ => track.NextPass.Start
            };
            var triggerTime = milestone.AddSeconds(rule.OffsetSeconds);
            if (Math.Abs((now - triggerTime).TotalSeconds) <= 3)
            {
                if (rule.LastTriggeredUtc.HasValue && (now - rule.LastTriggeredUtc.Value).TotalSeconds < 30) continue;
                rule.LastTriggeredUtc = now;
                TriggerAlarm(rule);
            }
        }
    }

    private void TriggerAlarm(AlarmRule rule)
    {
        if (rule.Action is AlarmActionType.Audio or AlarmActionType.All)
        {
            if (!string.IsNullOrEmpty(rule.AudioPath))
                Task.Run(() => AudioPlayer.Play(rule.AudioPath));
        }
        if (rule.Action is AlarmActionType.Script or AlarmActionType.All)
        {
            if (!string.IsNullOrEmpty(rule.ScriptPath))
                ScriptRunner.Run(rule.ScriptPath);
        }
        if (rule.Action is AlarmActionType.Flash or AlarmActionType.All)
        {
            Dispatcher.Invoke(() => new FlashOverlay(5).Show());
        }
    }

    private void Map_Loaded(object sender, RoutedEventArgs e)
    {
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var w = new SettingsWindow(_cfg);
        if (w.ShowDialog() == true)
        {
            ConfigService.Save(_cfg);
            if (MaidenheadConverter.TryParse(_cfg.Maidenhead, out var lat, out var lon))
            {
                _cfg.Latitude = lat; _cfg.Longitude = lon;
            }
            Map.Position = new PointLatLng(_cfg.Latitude, _cfg.Longitude);
            RebuildTracks();
            DrawOverlays();
        }
    }

    private void UpdateTle_Click(object sender, RoutedEventArgs e)
    {
        _ = AutoUpdateTleAsync();
    }
}
