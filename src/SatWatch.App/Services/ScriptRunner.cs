using System.IO;
using System.Diagnostics;

namespace SatWatch.App.Services;

public static class ScriptRunner
{
    public static void Run(string path)
    {
        if (!System.IO.File.Exists(path)) return;
        var ext = Path.GetExtension(path).ToLowerInvariant();
        ProcessStartInfo psi;
        if (ext == ".ps1")
            psi = new ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File \"{path}\"") { UseShellExecute = false, CreateNoWindow = true };
        else if (ext == ".bat" || ext == ".cmd")
            psi = new ProcessStartInfo("cmd.exe", $"/c \"{path}\"") { UseShellExecute = false, CreateNoWindow = true };
        else
            psi = new ProcessStartInfo(path) { UseShellExecute = true };
        Process.Start(psi);
    }
}

