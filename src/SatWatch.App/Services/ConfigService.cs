using System.IO;
using System.Text.Json;
using SatWatch.App.Models;

namespace SatWatch.App.Services;

public static class ConfigService
{
    private static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SatWatch");
    private static readonly string File = Path.Combine(Dir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (System.IO.File.Exists(File))
            {
                var json = System.IO.File.ReadAllText(File);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null)
                {
                    if (cfg.Favorites.Count == 0)
                    {
                        cfg.Favorites.Add(new FavoriteSatellite { Name = "ISS (ZARYA)", NoradId = 25544, Enabled = true, TleLine1 = "1 25544U 98067A   26241.65974551  .00005709  00000+0  11198-3 0  9990", TleLine2 = "2 25544  51.6318 296.4399 0004994  88.0334 272.1226 15.48929126583140" });
                        cfg.Favorites.Add(new FavoriteSatellite { Name = "SO-50", NoradId = 27607, Enabled = true, TleLine1 = "1 27607U 02058C 26289.11984649 .00000915 00000-0 12533-3 0  9998", TleLine2 = "2 27607 64.5506 114.7599 0070056 240.1080 119.3054 14.83261449282398" });
                        cfg.Favorites.Add(new FavoriteSatellite { Name = "AO-91", NoradId = 43137, Enabled = true, TleLine1 = "1 43017U 17073E 26242.86637184 .00007964 00000-0 33787-3 0  9999", TleLine2 = "2 43017 97.4582 108.5568 0149454  70.7425 290.9906 15.13699126476686" });
                    }
                    foreach (var f in cfg.Favorites)
                    {
                        if (string.IsNullOrWhiteSpace(f.TleLine1))
                        {
                            if (f.NoradId == 25544) { f.TleLine1 = "1 25544U 98067A   26241.65974551  .00005709  00000+0  11198-3 0  9990"; f.TleLine2 = "2 25544  51.6318 296.4399 0004994  88.0334 272.1226 15.48929126583140"; }
                            else if (f.NoradId == 27607) { f.TleLine1 = "1 27607U 02058C 26289.11984649 .00000915 00000-0 12533-3 0  9998"; f.TleLine2 = "2 27607 64.5506 114.7599 0070056 240.1080 119.3054 14.83261449282398"; }
                            else if (f.NoradId == 43137) { f.TleLine1 = "1 43017U 17073E 26242.86637184 .00007964 00000-0 33787-3 0  9999"; f.TleLine2 = "2 43017 97.4582 108.5568 0149454  70.7425 290.9906 15.13699126476686"; }
                        }
                    }
                    Save(cfg);
                    return cfg;
                }
            }
        }
        catch { }
        var c = new AppConfig();
        if (MaidenheadConverter.TryParse(c.Maidenhead, out var lat, out var lon))
        {
            c.Latitude = lat; c.Longitude = lon;
        }
        if (c.Favorites.Count == 0)
        {
            c.Favorites.Add(new FavoriteSatellite { Name = "ISS (ZARYA)", NoradId = 25544, Enabled = true, TleLine1 = "1 25544U 98067A   26241.65974551  .00005709  00000+0  11198-3 0  9990", TleLine2 = "2 25544  51.6318 296.4399 0004994  88.0334 272.1226 15.48929126583140" });
            c.Favorites.Add(new FavoriteSatellite { Name = "SO-50", NoradId = 27607, Enabled = true, TleLine1 = "1 27607U 02058C 26289.11984649 .00000915 00000-0 12533-3 0  9998", TleLine2 = "2 27607 64.5506 114.7599 0070056 240.1080 119.3054 14.83261449282398" });
            c.Favorites.Add(new FavoriteSatellite { Name = "AO-91", NoradId = 43137, Enabled = true, TleLine1 = "1 43017U 17073E 26242.86637184 .00007964 00000-0 33787-3 0  9999", TleLine2 = "2 43017 97.4582 108.5568 0149454  70.7425 290.9906 15.13699126476686" });
        }
        return c;
    }

    public static void Save(AppConfig cfg)
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        System.IO.File.WriteAllText(File, json);
    }
}

