using System.IO;
using SGPdotNET.CoordinateSystem;
using SGPdotNET.Observation;
using SGPdotNET.TLE;
using SGPdotNET.Util;
using SatWatch.App.Models;

namespace SatWatch.App.Services;

public class SatelliteTrack
{
    public FavoriteSatellite Favorite { get; set; } = null!;
    public Satellite Satellite { get; set; } = null!;
    public GroundStation Ground { get; set; } = null!;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double FootprintKm { get; set; }
    public List<(double lat, double lon)> Footprint { get; set; } = new();
    public List<(double lat, double lon)> Path { get; set; } = new();
    public SatelliteVisibilityPeriod? NextPass { get; set; }
    public List<SatelliteVisibilityPeriod> Passes { get; set; } = new();
}

public static class TrackerService
{
    public static SatelliteTrack? Build(FavoriteSatellite fav, double homeLat, double homeLon)
    {
        if (string.IsNullOrWhiteSpace(fav.TleLine1) || string.IsNullOrWhiteSpace(fav.TleLine2)) return null;
        try
        {
            var tle = new Tle(fav.Name, fav.TleLine1, fav.TleLine2);
            var sat = new Satellite(tle);
            var gs = new GroundStation(new GeodeticCoordinate(Angle.FromDegrees(homeLat), Angle.FromDegrees(homeLon), 0));
            var track = new SatelliteTrack { Favorite = fav, Satellite = sat, Ground = gs };
            UpdatePosition(track);
            return track;
        }
        catch { return null; }
    }

    public static void UpdatePosition(SatelliteTrack track)
    {
        try
        {
            var eci = track.Satellite.Predict(DateTime.UtcNow);
            var geo = eci.ToGeodetic();
            track.Lat = geo.Latitude.Degrees;
            track.Lon = geo.Longitude.Degrees;
            track.FootprintKm = eci.GetFootprint();
            track.Footprint = eci.GetFootprintBoundary(36).Select(g => (g.Latitude.Degrees, g.Longitude.Degrees)).ToList();
        }
        catch { }
    }

    public static void ComputePasses(SatelliteTrack track, DateTime fromUtc, DateTime toUtc)
    {
        try
        {
            var periods = track.Ground.Observe(track.Satellite, fromUtc, toUtc, TimeSpan.FromSeconds(60), Angle.FromDegrees(0), false, false, 15);
            track.Passes = periods.OrderBy(p => p.Start).ToList();
            track.NextPass = track.Passes.FirstOrDefault();
            if (track.NextPass != null)
            {
                track.Path.Clear();
                var p = track.NextPass;
                var t = p.Start;
                while (t <= p.End)
                {
                    var eci = track.Satellite.Predict(t);
                    var geo = eci.ToGeodetic();
                    track.Path.Add((geo.Latitude.Degrees, geo.Longitude.Degrees));
                    t = t.AddMinutes(1);
                }
            }
        }
        catch { }
    }
}

