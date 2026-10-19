using System.IO;
using System.Media;
using NAudio.Wave;

namespace SatWatch.App.Services;

public static class AudioPlayer
{
    public static void Play(string path)
    {
        if (!System.IO.File.Exists(path)) return;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext == ".wav")
        {
            using var sp = new SoundPlayer(path);
            sp.PlaySync();
        }
        else
        {
            using var reader = new AudioFileReader(path);
            using var output = new WaveOutEvent();
            output.Init(reader);
            output.Play();
            while (output.PlaybackState == PlaybackState.Playing) Thread.Sleep(100);
        }
    }
}

