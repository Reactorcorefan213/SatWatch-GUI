# SatWatch-GUI

Native Windows WPF satellite tracker for a secondary monitor.

## Features
- Real-time 2D map (OpenStreetMap via GMap.NET) with home pin, satellite markers, footprints, and pass paths.
- Maidenhead locator (4/6/8) to lat/lon.
- Favorite satellites with manual TLE entry.
- Automated TLE updates from a URL (default Celestrak) on a configurable interval.
- Pass prediction (AOS/TCA/LOS) using SGP.NET.
- Rule-based alarms with audio, script, and flash actions.

## Prerequisites
- Windows 10/11
- .NET 8 Desktop Runtime
- (Optional) Inno Setup 6 for installer

## Build
```bash
dotnet restore
dotnet build -c Release
```

## Run
```bash
dotnet run --project src/SatWatch.App
```

## Publish single-file
```bash
dotnet publish src/SatWatch.App -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Notes
- Config stored in %APPDATA%\SatWatch\config.json.
- GMap.NET caches tiles locally.

## Contributors
- Reactorcorefan213
- opencode (AI coding assistant)
