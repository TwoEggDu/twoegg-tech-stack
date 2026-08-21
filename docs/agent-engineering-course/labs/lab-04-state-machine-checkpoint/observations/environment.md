# Lab 04 execution environment

- Captured: `2026-08-21T13:22:28+08:00`
- OS: `Microsoft Windows 10.0.19045`
- Architecture: `X64 / win-x64`
- Timezone: `China Standard Time / UTC+08:00`
- SDK: `.NET SDK 10.0.301` (`96856fd726`)
- MSBuild: `18.6.4+96856fd72`
- Host: `.NET 10.0.9 x64` (`901ca94124`)
- TFM: `net10.0`
- Provider / model: `NONE / NONE`
- Package sources: `NuGet.Config` contains only `<clear />`
- Credentials: not read or required
- Runtime network surface: zero authored `System.Net` usage and zero `System.Net*` runtime assembly references, verified by `static-contract`
- Working directory: `E:\workspace\TechStackShow\docs\agent-engineering-course\labs\lab-04-state-machine-checkpoint`
- Full SDK capture: `observations/dotnet-info.txt`

The first OS metadata probe through `Get-CimInstance Win32_OperatingSystem` returned `Access denied`. This did not block the frozen OS requirement because `dotnet --info` and `RuntimeInformation` independently reported Windows `10.0.19045`, `win-x64` / `X64`. The access-denied output is retained in `execution-log.md` as unexpected behavior.
