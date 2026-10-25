# Window Throw

Zero-config keyboard-driven window snapper for Windows. Inspired by [Rectangle](https://rectangleapp.com) on macOS — press a shortcut, window moves. No setup required.

## Features

- **15 snap positions** via `Ctrl+Alt` shortcuts
- **Cycling** — press Left/Right Half repeatedly to step through thirds
- **Window Throw** — visual overlay to pick any position with your cursor
- **Stash** — slide windows off screen edges; move your cursor to the edge to bring them back
- Runs as a system tray app, no window, no taskbar entry

## Shortcuts

| Action | Shortcut |
|---|---|
| Window Throw overlay | `Ctrl+Alt+W` |
| Left Half | `Ctrl+Alt+←` |
| Right Half | `Ctrl+Alt+→` |
| Top Half | `Ctrl+Alt+↑` |
| Bottom Half | `Ctrl+Alt+↓` |
| Top Left / Top Right | `Ctrl+Alt+U` / `Ctrl+Alt+I` |
| Bottom Left / Bottom Right | `Ctrl+Alt+J` / `Ctrl+Alt+K` |
| Left Third / Center Third / Right Third | `Ctrl+Alt+D` / `F` / `G` |
| Left ⅔ / Right ⅔ | `Ctrl+Alt+E` / `Ctrl+Alt+T` |
| Maximize | `Ctrl+Alt+Enter` |
| Center | `Ctrl+Alt+C` |
| Restore | `Ctrl+Alt+Backspace` |
| Previous Display / Next Display | `Ctrl+Alt+Shift+←` / `→` |

### Cycling

Pressing `Ctrl+Alt+←` (Left Half) repeatedly within 2 seconds cycles through:

```
Left Half → Left Third → Left Two-Thirds → Left Half → ...
```

Same applies to `Ctrl+Alt+→` for the right side.

### Window Throw

Press `Ctrl+Alt+W` to open the overlay. Move your cursor over the desired zone, then release `Ctrl+Alt` to snap — or click. Press `ESC` to cancel.

### Stash

Right-click the tray icon → **Stash** to slide the foreground window off a screen edge, leaving a thin tab visible. Move your cursor to that screen edge to bring it back. **Unstash All** restores every stashed window at once.

## Requirements

- Windows 10 or 11
- [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) (Desktop Runtime)

## Build

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `bin/Release/net8.0-windows/win-x64/publish/WindowThrow.exe`

For a self-contained single exe (no runtime required):

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
