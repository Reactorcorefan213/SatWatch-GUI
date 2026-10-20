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
                if (cfg != null) return cfg;
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
            c.Favorites.Add(new FavoriteSatellite { Name = "ISS (ZARYA)", NoradId = 25544, Enabled = true });
            c.Favorites.Add(new FavoriteSatellite { Name = "SO-50", NoradId = 27607, Enabled = true });
            c.Favorites.Add(new FavoriteSatellite { Name = "AO-91", NoradId = 43137, Enabled = true });
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

