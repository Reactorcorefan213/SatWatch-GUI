using System.IO;
using System.Net.Http;
using SatWatch.App.Models;

namespace SatWatch.App.Services;

public static class TleService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    public static async Task<string> FetchRawAsync(string url)
    {
        return await Http.GetStringAsync(url);
    }

    public static List<FavoriteSatellite> ParseTleText(string text)
    {
        var list = new List<FavoriteSatellite>();
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 2 < lines.Length; i += 3)
        {
            var name = lines[i].Trim();
            var l1 = lines[i + 1].Trim();
            var l2 = lines[i + 2].Trim();
            if (!l1.StartsWith("1 ") || !l2.StartsWith("2 ")) { i -= 2; continue; }
            uint norad = 0;
            if (l1.Length > 7) uint.TryParse(l1.Substring(2, 5).Trim(), out norad);
            list.Add(new FavoriteSatellite { Name = name, NoradId = norad, TleLine1 = l1, TleLine2 = l2, Enabled = false });
        }
        return list;
    }

    public static void UpdateFavorites(AppConfig cfg, List<FavoriteSatellite> fetched)
    {
        foreach (var fav in cfg.Favorites)
        {
            var match = fetched.FirstOrDefault(f => f.NoradId == fav.NoradId && fav.NoradId != 0)
                        ?? fetched.FirstOrDefault(f => f.Name.Equals(fav.Name, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                fav.TleLine1 = match.TleLine1;
                fav.TleLine2 = match.TleLine2;
            }
        }
    }
}

