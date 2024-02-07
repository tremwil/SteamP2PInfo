namespace SteamP2PInfo
{
    internal class SteamPeerInfo
    {
        internal SteamPeerBase peer = null;
        internal bool isConnected;
        internal long lastDisconnectTimeMS = 0;
        

        internal SteamPeerInfo(SteamPeerBase peer)
        {
            this.peer = peer;
            isConnected = peer != null;
        }
    }
}
