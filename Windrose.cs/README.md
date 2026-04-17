# WindowsGSM.Windrose

WindowsGSM plugin for hosting a **Windrose Dedicated Server**.

## Features

- Install and update Windrose Dedicated Server through SteamCMD
- Launch the real Windrose server binary directly
- Read standard server settings from WindowsGSM
- Update `ServerDescription.json` before startup
- Apply:
  - Server name
  - Max player count
  - Password enabled / disabled
  - Password
  - P2P proxy address from the WindowsGSM Server IP field
  - World ID selection
  - Invite code
- Detect available worlds from `WorldDescription.json`
- Show invite code and detected worlds in the WindowsGSM notice log
- Embedded console support

## Important notes

This plugin launches:

`R5\Binaries\Win64\WindroseServer-Win64-Shipping.exe`

rather than the wrapper executable.

That helps WindowsGSM track the server process more reliably.

Windrose stores important settings in JSON rather than relying only on launch parameters, so this plugin updates `ServerDescription.json` before the server starts.

## What WindowsGSM fields are used

The plugin uses the normal WindowsGSM server edit fields for:

- **Server Name**
- **Server IP Address**
- **Server Port**
- **Server Query Port**
- **Server Maxplayer**
- **Server Start Param**

These are applied when the server is started.

## Windrose-specific behavior

The plugin updates values inside `ServerDescription.json`, including:

- `ServerName`
- `MaxPlayerCount`
- `IsPasswordProtected`
- `Password`
- `InviteCode`
- `WorldIslandId`
- `P2pProxyAddress`

It also scans the Windrose worlds directory and logs detected worlds in the format:

```text
Invite Code: xxxxxxxx
Detected Windrose worlds:
- WORLDID | WorldName | Preset