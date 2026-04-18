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
  - P2P proxy address from the WindowsGSM **Server IP Address** field
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

## Installation

### Install WindowsGSM
1. Download WindowsGSM from the official site.
2. Create a folder for WindowsGSM and your servers.
3. Place `WindowsGSM.exe` in that folder.
4. Run WindowsGSM.

WindowsGSM website:  
https://windowsgsm.com/

### Install the plugin
You can install the plugin in either of these ways.

#### Option 1: Import ZIP through WindowsGSM
1. Download the latest release from this repository.
2. Open WindowsGSM.
3. Go to the **Plugins** section.
4. Import the ZIP file.
5. Reload plugins or restart WindowsGSM.

#### Option 2: Manual install
1. Download the latest release from this repository.
2. Extract it.
3. Place the plugin folder into the WindowsGSM `plugins` folder.
4. Reload plugins or restart WindowsGSM.

## Installing the Windrose server
1. Open WindowsGSM.
2. Click **Install Game Server**.
3. Search for **Windrose Dedicated Server**.
4. Install the server.

## First start

On first launch, Windrose may generate its own config files.

If `ServerDescription.json` does not exist yet, start the server once, let Windrose generate the files, then stop and start it again.

## Recommended settings

Typical values:

- **Server IP Address**: your public or reachable server IP
- **Server Port**: your chosen game port
- **Server Query Port**: your chosen query port
- **Server Maxplayer**: your chosen player count
- **Server Start Param**: optional extra startup arguments

## Known limitations

- Windrose networking appears to rely on P2P / NAT punch-through behavior, so status and query handling may not behave like a traditional fixed-port dedicated server.
- The plugin can reliably report the **requested** server and query ports, but not always the exact runtime public listener behavior exposed by the game.
- Some Windrose-specific settings are still not mapped to standard WindowsGSM fields.

## Useful links

### Official Windrose dedicated server guide
https://playwindrose.com/dedicated-server-guide/

### Community setup guide
https://techraptor.net/gaming/guides/windrose-server-setup

### Windrose on Steam
https://store.steampowered.com/app/3041230/Windrose/

### Dedicated server app on SteamDB
https://steamdb.info/app/4129620/info/

## License

This project is licensed under the MIT License.

See the `LICENSE` file for details.
