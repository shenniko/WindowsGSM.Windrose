# WindowsGSM.Windrose

WindowsGSM plugin for hosting a **Windrose Dedicated Server**.

## Features

- Install and update Windrose Dedicated Server through SteamCMD
- Launch the real Windrose server binary directly
- Read standard server settings from WindowsGSM
- Update supported values in `ServerDescription.json` before startup
- Apply:
  - Server name
  - Max player count
  - P2P proxy address from the WindowsGSM Server IP field
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

## ServerDescription.json support

The plugin currently updates the following values from WindowsGSM before server start:

- `ServerName`
- `MaxPlayerCount`
- `P2pProxyAddress`

It also reads and displays:

- `InviteCode`
- detected worlds from `WorldDescription.json`