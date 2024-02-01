# SteamP2PInfo
Simple C# application displaying active Steam P2P connection info, namely SteamID / ping / connection quality. This was specifically made with Elden Ring in mind, but it should work for any Steam Networking game that authenticates peers using `ISteamUser::BeginAuthSession`. Also comes with a customizable overlay (**windowed / borderless mode only!**) and logging.

**It also supports adding peers to the Steam recent players list, if the game does not support this.**
## [Releases](https://github.com/tremwil/SteamP2PInfo/releases/)

![](https://raw.githubusercontent.com/tremwil/SteamP2PInfo/master/overlay_er.PNG)
![](https://raw.githubusercontent.com/tremwil/SteamP2PInfo/master/gui.PNG)

# How to Use
Download the lastest release from the Releases tab and extract the ZIP file in any folder on your computer. Once the game is running, start `SteamP2PInfo.exe` and click on "Attach Game". Select the appropriate game window in the dialog. If this game has never been opened before, you will be prompted to enter the game's **Steam AppId**. This can be queried on websites like [steamdb](https://steamdb.info/). The Steam console will then open. **You must enter the following command in the console for the tool to work:** `log_ipc "BeginAuthSession,EndAuthSession"`. The program should now be ready! You can then go in the "Config" tab to customize game-specific settings. 

# Known Issues (V1.0.1)
### Peers not getting detected in rare circumstances
This is due to the very naive Steam IPC log file parsing. The program can "miss" a Steam lobby, preventing the detection of P2P peers in this lobby. I plan to improve the log file parsing to make this rarer or completely eliminate it in the future.

### Overlay cannot be dragged around
I'm not sure what the cause for this is yet. Please modify the "X Offset" and "Y Offset" settings directly for now.

### Cannot customize ping color thresholds
Not really an issue, but I plan to implement a GUI editor for this in the future. For now the colors can be changed by directly editing the game's json configuration file. 

# FAQ
### Why does it require administrator privileges?
While the `SteamNetworkingMessages` API provides detailed connection information, the old API `SteamNetworking` does not do this. Hence in this the pings are computed by monitoring STUN packets that are sent to and recieved from the players' IPs. To capture these packets I use Event Tracing for Windows (ETW), which requires administrator privileges for "kernel" events like networking.

### Why do I have to use the Steam console / IPC logging? Isn't there an cleaner way to monitor lobbies?

Sadly, this is the only way I found to reliably detect lobby joining and creation when running two processes using the same game ID. I cannot use Steam callbacks, because if the game "consumes" them my tool's callbacks will not be called, and vice versa. I also do not want to rely on reading game memory or injecting code into the game in order to support anti-cheat protected games. In the future, I plan to move from IPC log file parsing to an internal `steam.exe` hook to make peer detection 100% reliable. Since some VAC games might not like this, an option will be available to use the legacy system if needed. This new method will take quite a bit of work to implement, however.

*(1.1.0 and above)* [zkxs](https://github.com/zkxs) had the smart idea to log `ISteamUser::BeginAuthSession` calls instead of lobby joining. Since Steam Networking P2P games must use this method to authenticate peers. This is a much more robust choice than the old logic which was using `IClientMatchmaking` `CreateLobby` and `JoinLobby`.

### How is the "Connection Quality" computed?
When a peer is connected using the `SteamNetworkingMessages` API, this roughly corresponds to `1 - packet_loss`. When connected using the deprecated API `SteamNetworking`, the value is computed using the formula `1 / (0.01 * jitter + 1)`, where `jitter` is the standard deviation of the last 10 ping values. This is done instead of showing `jitter` directly so that the value is on the same scale as the `SteamNetworkingMessages` one.

### Why does the program close with the game?
Since the tool is loaded with the game's Steam AppId, letting the program run after the game closes would make Steam think the game is still running. Calling `SteamAPI_Shutdown` does not seem to fix the problem, so we have to close the process.

### I found a bug / I have something to say about the tool
Feel free to open an issue on this Github repo.
