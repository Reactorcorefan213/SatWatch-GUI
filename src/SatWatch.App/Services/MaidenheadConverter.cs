namespace SatWatch.App.Services;

public static class MaidenheadConverter
{
    public static bool TryParse(string locator, out double lat, out double lon)
    {
        lat = 0; lon = 0;
        if (string.IsNullOrWhiteSpace(locator)) return false;
        locator = locator.Trim().ToUpperInvariant();
        if (locator.Length != 4 && locator.Length != 6 && locator.Length != 8) return false;

        // Field
        if (locator[0] < 'A' || locator[0] > 'R') return false;
        if (locator[1] < 'A' || locator[1] > 'R') return false;
        if (!char.IsDigit(locator[2]) || !char.IsDigit(locator[3])) return false;

        lon = (locator[0] - 'A') * 20.0 - 180.0;
        lat = (locator[1] - 'A') * 10.0 - 90.0;

        lon += (locator[2] - '0') * 2.0;
        lat += (locator[3] - '0') * 1.0;

        if (locator.Length >= 6)
        {
            if (locator[4] < 'A' || locator[4] > 'X') return false;
            if (locator[5] < 'A' || locator[5] > 'X') return false;
            lon += (locator[4] - 'A') * (5.0 / 60.0); // 5 min
            lat += (locator[5] - 'A') * (2.5 / 60.0);
            if (locator.Length == 6)
            {
                // center of subsquare
                lon += (5.0 / 60.0) / 2.0;
                lat += (2.5 / 60.0) / 2.0;
            }
        }
        if (locator.Length == 8)
        {
            if (!char.IsDigit(locator[6]) || !char.IsDigit(locator[7])) return false;
            lon += (locator[6] - '0') * (5.0 / 60.0 / 10.0); // 0.5 min? approx
            lat += (locator[7] - '0') * (2.5 / 60.0 / 10.0);
            // center
            lon += (5.0 / 60.0 / 10.0) / 2.0;
            lat += (2.5 / 60.0 / 10.0) / 2.0;
        }
        else if (locator.Length == 4)
        {
            lon += 1.0; // half of 2 deg
            lat += 0.5;
        }
        return true;
    }
}
