using System.Windows;
using SatWatch.App.Models;

namespace SatWatch.App;

public partial class SettingsWindow : Window
{
    private readonly AppConfig _cfg;

    public SettingsWindow(AppConfig cfg)
    {
        InitializeComponent();
        _cfg = cfg;
        MaidenBox.Text = cfg.Maidenhead;
        TleUrlBox.Text = cfg.TleUpdateUrl;
        TleDaysBox.Text = cfg.TleUpdateDays.ToString();
        AutoUpdateBox.IsChecked = cfg.AutoUpdateTle;

        AlarmMilestoneBox.ItemsSource = Enum.GetValues(typeof(AlarmMilestone));
        AlarmActionBox.ItemsSource = Enum.GetValues(typeof(AlarmActionType));

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
        DialogResult = true;
    }
}
