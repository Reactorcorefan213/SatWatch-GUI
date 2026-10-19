using System.Windows;
using System.Windows.Threading;
using SatWatch.App.Models;
using SatWatch.App.Services;
using SatWatch.App.Views;

namespace SatWatch.App;

public partial class MainWindow : Window
{
    private AppConfig _cfg = ConfigService.Load();
    private readonly List<SatelliteTrack> _tracks = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(5) };

    private const double MapW = 1024;
    private const double MapH = 512;

    public MainWindow()
    {
        InitializeComponent();
        if (MaidenheadConverter.TryParse(_cfg.Maidenhead, out var lat, out var lon))
        {
            _cfg.Latitude = lat; _cfg.Longitude = lon;
        }
        Loaded += (_, _) => { RebuildTracks(); DrawOverlays(); };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
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

    private static double LonToX(double lon) => (lon + 180.0) / 360.0 * MapW;
    private static double LatToY(double lat) => (90.0 - lat) / 180.0 * MapH;

    private void DrawOverlays()
    {
        Overlay.Children.Clear();

        var bs = new System.Windows.Shapes.Polygon
        {
            Points = new System.Windows.Media.PointCollection { new System.Windows.Point(0, -12), new System.Windows.Point(10, 8), new System.Windows.Point(-10, 8) },
            Fill = System.Windows.Media.Brushes.OrangeRed,
            Stroke = System.Windows.Media.Brushes.White,
            StrokeThickness = 1
        };
        System.Windows.Controls.Canvas.SetLeft(bs, LonToX(_cfg.Longitude));
        System.Windows.Controls.Canvas.SetTop(bs, LatToY(_cfg.Latitude));
        Overlay.Children.Add(bs);

        foreach (var track in _tracks)
        {
            if (track.Footprint.Count > 2)
            {
                var poly = new System.Windows.Shapes.Polygon
                {
                    Stroke = System.Windows.Media.Brushes.Cyan,
                    StrokeThickness = 1,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Points = new System.Windows.Media.PointCollection(track.Footprint.Select(p => new System.Windows.Point(LonToX(p.lon), LatToY(p.lat))))
                };
                Overlay.Children.Add(poly);
            }

            if (track.Path.Count > 1)
            {
                var line = new System.Windows.Shapes.Polyline
                {
                    Stroke = System.Windows.Media.Brushes.Lime,
                    StrokeThickness = 2,
                    Points = new System.Windows.Media.PointCollection(track.Path.Select(p => new System.Windows.Point(LonToX(p.lon), LatToY(p.lat))))
                };
                Overlay.Children.Add(line);
            }

            var dot = new System.Windows.Shapes.Ellipse { Width = 8, Height = 8, Fill = System.Windows.Media.Brushes.Lime, Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 1 };
            System.Windows.Controls.Canvas.SetLeft(dot, LonToX(track.Lon) - 4);
            System.Windows.Controls.Canvas.SetTop(dot, LatToY(track.Lat) - 4);
            Overlay.Children.Add(dot);

            var label = new System.Windows.Controls.TextBlock { Text = track.Favorite.Name, Foreground = System.Windows.Media.Brushes.Yellow, FontSize = 10, FontWeight = FontWeights.Bold };
            System.Windows.Controls.Canvas.SetLeft(label, LonToX(track.Lon) + 6);
            System.Windows.Controls.Canvas.SetTop(label, LatToY(track.Lat) - 6);
            Overlay.Children.Add(label);
        }
    }

    private void Tick()
    {
        foreach (var t in _tracks)
            TrackerService.UpdatePosition(t);
        DrawOverlays();
        UpdatePassList();
        RunAlarms();
        StatusText.Text = $"Home: {_cfg.Maidenhead} | Sats: {_tracks.Count} | {DateTime.Now:T}";
        if (_cfg.AutoUpdateTle && (DateTime.UtcNow - _cfg.LastTleUpdateUtc).TotalDays >= _cfg.TleUpdateDays)
            _ = AutoUpdateTleAsync();
    }

    private void UpdatePassList()
    {
        var items = new List<string>();
        foreach (var t in _tracks)
            foreach (var p in t.Passes.Take(3))
                items.Add($"{t.Favorite.Name}: AOS {p.Start.ToLocalTime():g} TCA {p.MaxElevationTime.ToLocalTime():t} LOS {p.End.ToLocalTime():t}");
        PassList.ItemsSource = items;
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
            DrawOverlays();
        }
        catch { }
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
            if (!string.IsNullOrEmpty(rule.AudioPath))
                Task.Run(() => AudioPlayer.Play(rule.AudioPath));
        if (rule.Action is AlarmActionType.Script or AlarmActionType.All)
            if (!string.IsNullOrEmpty(rule.ScriptPath))
                ScriptRunner.Run(rule.ScriptPath);
        if (rule.Action is AlarmActionType.Flash or AlarmActionType.All)
            Dispatcher.Invoke(() => new FlashOverlay(5).Show());
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
            RebuildTracks();
            DrawOverlays();
        }
    }

    private void UpdateTle_Click(object sender, RoutedEventArgs e) => _ = AutoUpdateTleAsync();
}
