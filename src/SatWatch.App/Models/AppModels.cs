using System.Text.Json.Serialization;

namespace SatWatch.App.Models;

public class FavoriteSatellite
{
    public string Name { get; set; } = "";
    public uint NoradId { get; set; }
    public string TleLine1 { get; set; } = "";
    public string TleLine2 { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public enum AlarmMilestone { AOS, TCA, LOS }

public enum AlarmActionType { Audio, Script, Flash, All }

public class AlarmRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SatelliteName { get; set; } = "";
    public AlarmMilestone Milestone { get; set; } = AlarmMilestone.AOS;
    public int OffsetSeconds { get; set; } = 0; // negative = before
    public AlarmActionType Action { get; set; } = AlarmActionType.Flash;
    public string AudioPath { get; set; } = "";
    public string ScriptPath { get; set; } = "";
    public bool Enabled { get; set; } = true;
    [JsonIgnore]
    public DateTime? LastTriggeredUtc { get; set; }
}

public class AppConfig
{
    public string Maidenhead { get; set; } = "FN20ab";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public List<FavoriteSatellite> Favorites { get; set; } = new();
    public string TleUpdateUrl { get; set; } = "https://celestrak.org/NORAD/elements/gp.php?GROUP=visual&FORMAT=tle";
    public int TleUpdateDays { get; set; } = 12;
    public DateTime LastTleUpdateUtc { get; set; } = DateTime.MinValue;
    public List<AlarmRule> Alarms { get; set; } = new();
    public bool AutoUpdateTle { get; set; } = true;
}
