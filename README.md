# Forever Launcher

WPF launcher for the WoW Classic 1.60.1 client on private servers. Starts `WowB.exe`,
points it at your portal and swaps in your server's public key.

## Build
    cd src
    dotnet build -c Release

Output: `src/bin/Release/net48/ForeverLauncher.exe`

## Configure
Edit `servers.json` next to the exe (an example is written on first run):
`portal`, `publicKey`, `regionGroup`, `loginPort`, `worldPort`, `realms`.

Self-update is disabled in this fork.
