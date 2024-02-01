namespace SteamP2PInfo
{
    internal class SteamPeerInfo
    {
        internal SteamPeerBase steamPeerBase;
        internal DisconnectReason disconnectReason;

        internal SteamPeerInfo(SteamPeerBase steamPeerBase)
        {
            this.steamPeerBase = steamPeerBase;
            disconnectReason = DisconnectReason.NONE;
        }

        internal enum DisconnectReason
        {
            NONE,
            AUTH_SESSION_ENDED,
            PEER_DISCONNECTED,
        }
    }
}
