using System.Windows;

namespace SatWatch.App.Services;

public static class ThemeService
{
    public static void Apply(string theme)
    {
        var app = Application.Current;
        var dicts = app.Resources.MergedDictionaries;
        // remove existing theme dictionaries
        for (int i = dicts.Count - 1; i >= 0; i--)
        {
            var src = dicts[i].Source?.OriginalString ?? "";
            if (src.Contains("Themes/")) dicts.RemoveAt(i);
        }
        var uri = new Uri($"pack://application:,,,/Themes/{theme}.xaml", UriKind.Absolute);
        dicts.Add(new ResourceDictionary { Source = uri });
    }
}
