using SatWatch.App.Services;
using System.Windows.Controls;
using System.Windows;
using SatWatch.App.Models;

namespace SatWatch.App;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;

    private List<FavoriteSatellite> _browsed = new();
    private List<FavoriteSatellite> _filtered = new();

    public SettingsWindow(AppConfig cfg)
    {
        ThemeService.Apply(cfg.Theme ?? "Light");
        InitializeComponent();
        _cfg = cfg;
        MaidenBox.Text = cfg.Maidenhead;
        TleUrlBox.Text = cfg.TleUpdateUrl;
        TleDaysBox.Text = cfg.TleUpdateDays.ToString();
        AutoUpdateBox.IsChecked = cfg.AutoUpdateTle;
        ThemeBox.SelectedIndex = (cfg.Theme == "Dark") ? 1 : 0;

        AlarmMilestoneBox.ItemsSource = Enum.GetValues(typeof(AlarmMilestone));
        AlarmActionBox.ItemsSource = Enum.GetValues(typeof(AlarmActionType));

        TleSourceBox.ItemsSource = new List<string>
        {
            "Amateur Radio|https://celestrak.org/NORAD/elements/gp.php?GROUP=amateur&FORMAT=tle",
            "Weather|https://celestrak.org/NORAD/elements/gp.php?GROUP=weather&FORMAT=tle",
            "Stations (ISS)|https://celestrak.org/NORAD/elements/gp.php?GROUP=stations&FORMAT=tle",
            "Visual|https://celestrak.org/NORAD/elements/gp.php?GROUP=visual&FORMAT=tle",
            "GPS|https://celestrak.org/NORAD/elements/gp.php?GROUP=gps-ops&FORMAT=tle",
            "Starlink|https://celestrak.org/NORAD/elements/gp.php?GROUP=starlink&FORMAT=tle"
        };
        TleSourceBox.SelectedIndex = 0;

        RefreshSatList();
        RefreshAlarmList();
        AlarmSatBox.ItemsSource = cfg.Favorites.Select(f => f.Name).ToList();
    }

    private void RefreshSatList()
    {
        SatList.ItemsSource = null;
        SatList.ItemsSource = _cfg.Favorites.Select(f => $"{f.Name} ({f.NoradId})").ToList();
    }

    private void RefreshAlarmList()
    {
        AlarmList.ItemsSource = null;
        AlarmList.ItemsSource = _cfg.Alarms.Select(a => $"{a.SatelliteName} {a.Milestone} {a.OffsetSeconds}s {a.Action}").ToList();
    }

    private async void FetchTleList_Click(object sender, RoutedEventArgs e)
    {
        if (TleSourceBox.SelectedItem == null) return;
        var url = TleSourceBox.SelectedItem.ToString()!.Split('|')[1];
        try
        {
            FetchTleList.IsEnabled = false;
            var raw = await TleService.FetchRawAsync(url);
            _browsed = TleService.ParseTleText(raw);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Fetch failed: " + ex.Message);
        }
        finally
        {
            FetchTleList.IsEnabled = true;
        }
    }

    private void TleSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var q = (TleSearchBox.Text ?? "").Trim().ToLowerInvariant();
        _filtered = string.IsNullOrEmpty(q)
            ? _browsed
            : _browsed.Where(s => s.Name.ToLowerInvariant().Contains(q) || s.NoradId.ToString().Contains(q)).ToList();
        TleBrowseList.ItemsSource = _filtered.Select(s => $"{s.Name} ({s.NoradId})").ToList();
    }

    private void AddBrowsed_Click(object sender, RoutedEventArgs e)
    {
        if (TleBrowseList.SelectedIndex < 0 || TleBrowseList.SelectedIndex >= _filtered.Count) return;
        var sel = _filtered[TleBrowseList.SelectedIndex];
        if (_cfg.Favorites.Any(f => f.NoradId == sel.NoradId && sel.NoradId != 0)) return;
        _cfg.Favorites.Add(new FavoriteSatellite
        {
            Name = sel.Name,
            NoradId = sel.NoradId,
            TleLine1 = sel.TleLine1,
            TleLine2 = sel.TleLine2,
            Enabled = true
        });
        RefreshSatList();
        AlarmSatBox.ItemsSource = _cfg.Favorites.Select(f => f.Name).ToList();
    }

    private void AddSat_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NewSatName.Text)) return;
        uint norad = 0; uint.TryParse(NewSatNorad.Text, out norad);
        _cfg.Favorites.Add(new FavoriteSatellite
        {
            Name = NewSatName.Text.Trim(),
            NoradId = norad,
            TleLine1 = NewSatTle1.Text.Trim(),
            TleLine2 = NewSatTle2.Text.Trim(),
            Enabled = true
        });
        RefreshSatList();
        AlarmSatBox.ItemsSource = _cfg.Favorites.Select(f => f.Name).ToList();
    }

    private void RemoveSat_Click(object sender, RoutedEventArgs e)
    {
        if (SatList.SelectedIndex < 0 || SatList.SelectedIndex >= _cfg.Favorites.Count) return;
        _cfg.Favorites.RemoveAt(SatList.SelectedIndex);
        RefreshSatList();
        AlarmSatBox.ItemsSource = _cfg.Favorites.Select(f => f.Name).ToList();
    }

    private void AddAlarm_Click(object sender, RoutedEventArgs e)
    {
        if (AlarmSatBox.SelectedItem == null) return;
        int off = 0; int.TryParse(AlarmOffsetBox.Text, out off);
        _cfg.Alarms.Add(new AlarmRule
        {
            SatelliteName = AlarmSatBox.SelectedItem.ToString()!,
            Milestone = (AlarmMilestone)(AlarmMilestoneBox.SelectedItem ?? AlarmMilestone.AOS),
            OffsetSeconds = off,
            Action = (AlarmActionType)(AlarmActionBox.SelectedItem ?? AlarmActionType.Flash),
            AudioPath = AlarmAudioBox.Text.Trim(),
            ScriptPath = AlarmScriptBox.Text.Trim(),
            Enabled = true
        });
        RefreshAlarmList();
    }

    private void RemoveAlarm_Click(object sender, RoutedEventArgs e)
    {
        if (AlarmList.SelectedIndex < 0 || AlarmList.SelectedIndex >= _cfg.Alarms.Count) return;
        _cfg.Alarms.RemoveAt(AlarmList.SelectedIndex);
        RefreshAlarmList();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _cfg.Maidenhead = MaidenBox.Text.Trim();
        _cfg.TleUpdateUrl = TleUrlBox.Text.Trim();
        if (int.TryParse(TleDaysBox.Text, out var d)) _cfg.TleUpdateDays = d;
        _cfg.AutoUpdateTle = AutoUpdateBox.IsChecked == true;
        _cfg.Theme = (ThemeBox.SelectedIndex == 1) ? "Dark" : "Light";
        ThemeService.Apply(_cfg.Theme);
        DialogResult = true;
    }
}


