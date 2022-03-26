# DS3ConnectionInfo
Simple C# application showing active P2P connection information along ping and geolocation for Dark Souls III. In a game whose PvP is highly dependent on spacing and timing, knowing the latency between you and your opponent before the fight can be pretty useful. Also implements an in game overlay (**windowed or borderless windowed mode only**) and a simple ping filter (it has many limitations, see FAQ for more information).

## [Download](https://github.com/tremwil/DS3ConnectionInfo/releases/download/V4.3/DS3ConnectionInfo-V4.3.zip)

## DISCLAIMER: 
**Do not share the location information provided by this program. While you should be free to view it yourself since the players are connected to your computer, respect the privacy of others. I am not responsible for any misuse of this information.**

**I release the source code of the program for transparency and because C# is easy to decompile anyways. You are free to re-use parts of this code for other projects, but please give proper attribution.** 

![](https://s01.geekpic.net/di-L8U0SH.png)

# Installation
Download the lastest release from the Releases tab and extract the ZIP file in any folder on your computer. Start `DS3ConnectionInfo.exe` whenever you want to use it. If you restart the game, you will also have to restart the program.

# Known Issues (V4.1)

### My game crashed after I returned from another world / revived
If you are using the invasion hotkeys, this can happen if you queue an invasion during a loading screen. The program will try to prevent you from doing so, but there is a small window where it thinks you are no longer in a loading screen while queueing an invasion will still crash. Simply wait until your character is completely loaded. This will be fixed once I find a better indicator for when the game is in a loading screen.

### My settings are not getting saved
This is a bug in V4.1 where your settings will not be saved when the program closes with the game. To save your settings, configure everything and then close the program without the game running. See [Issue #9](https://github.com/tremwil/DS3ConnectionInfo/issues/9) for more information.

### The program crashes on startup / when I start Dark Souls III
This is most likely due to interference from anti-virus software. Although all the Windows API calls used by this program are not uncommon, some antivirus can be overly strict. Whitelisting the application should fix this issue. See [Issue #8](https://github.com/tremwil/DS3ConnectionInfo/issues/8) for more information.

# FAQ

### Is it bannable?
It is 100% ban-safe, as the program does not modify the game's original code in any way. The program does allocate new memory in the Dark Souls III process to execute in-game functions (eg. query an invasion), however, but this does not cause bans.

### How does the ping filter work?
The ping filter is very basic. It trigger when one attemps to join an online session (either through covenant invasions or summon signs). After the connection with all players is established, the program will wait "Filter Delay" seconds and then compute the average ping of every player in the lobby. If this value is higher than the maximum average threshold or the ping of a single player is higher than the maximum absolute threshold, the online session will be abandonned and your invasion request / summon sign will reset. While this works fairly well in practice, it has some disadvantages:
- Since it is impossible to know the team of a player before the loading screen, the connection to a friendly player will affect matchmaking. 
- It is still possible to connect to unacceptably laggy players, if they join the online session after the player using the ping filter. For the same reason, this ping filter is useless for hosts. **This is by design**. I did not want to implement any kind of targeted kick functionality into the program, especially since it is open source.

### Are the pings shown accurate if the other player is using a VPN?
**Yes**, if V2+ is used. Since the pings are not calculated by pinging the remote IP but rather by listening for STUN reply packets coming from the remote game, using a VPN will not show an incorrect ping. The location information, however, does use the remote IP, and can be hidden using a VPN.

### Why does it require administrator privileges? (V2+)
In V2+, the pings are computed by monitoring STUN packets that are sent to and recieved from the players' IPs. This is more accurate and updates faster than the traceroute method used in V1. However, to capture these packets I use Event Tracing for Windows (ETW), which requires administrator privileges for "kernel" events like networking. 

### Why does the program close when the game does?
Since the code uses the Steam API with DS3's Steam App ID, letting the program run after the game closes would make Steam think the it is still running. Calling `SteamAPI_Shutdown` does not seem to fix the problem, so we have to close the process. A DLL mod could make this seamless, but making an external program is simpler and doesn't require ban testing.

### Why show the location of players?
The ping system used in V1 could sometimes be inaccurate due to early network nodes blocking ping packets. Showing basic geolocation information could help to get a more reliable idea of the latency in that case. With V2+ this is no longer necessary, but since it is still possible to access the old release and source I have decided to keep this feature in. When playing any direct P2P game such as Dark Souls III you should be aware that your public IP address (which is linked to your location) is transmitted to other players. **This is not a security exploit.** Use a VPN if you wish to keep this information private.

### I found a bug / I have something to say about the mod
Feel free to open an issue on this Github or direct message me on discord at tremwil#3713.

# How it works (V2+)
The Steam API is used to query the Steam ID of recently met players. Using `GetP2PSessionState`, we are able to query if each player is currently connected and get the remote IP address. This is then matched to the slot and character name from the game's memory. The reason for using recently met players instead of simply reading the Steam ID from the game's memory is that the latter can be spoofed by players (for example when running the PyreProtecc anti cheat). To calculate the pings, ETW (Event Tracing for Windows) networking events are monitored to find when STUN packets are sent to and recieved from player IPs. The region-specific geolocating comes from [ip-api](https://ip-api.com).

# How it works (V1)
The program reads the Steam ID and character name of active players from the game's memory (like Cheat Engine). From there we use the Steam API function `GetP2PSessionState` to get the remote IP address. Since most routers deny ICMP ping requests, I use a traceroute like method to ping the network node that is closest to the player IP. This gives a pretty good estimate for the ping, but it will always be lower than the true value. Hence I also provide region-specific geolocating using [ip-api](https://ip-api.com) to query the country and region (state) information.

Credits to the developers of the DS3 Grand Archives Cheat Table for the player data pointers.
