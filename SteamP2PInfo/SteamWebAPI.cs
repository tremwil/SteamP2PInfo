using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Steamworks;

using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using SteamP2PInfo.Config;


namespace SteamP2PInfo
{
    static class SteamWebAPI
    {
        public static CSteamID GetMainSteamId(CSteamID altId)
        {
            if (Settings.Default.SteamWebApiKey == "")
                return new CSteamID(0);

            string query = $"http://api.steampowered.com/IPlayerService/IsPlayingSharedGame/v0001/?key={Settings.Default.SteamWebApiKey}&steamid={altId.m_SteamID}&appid_playing={GameConfig.Current.SteamAppId}&format=json";
            WebRequest req = WebRequest.Create(query);
            HttpWebResponse resp;

            try
            {
                resp = (HttpWebResponse)req.GetResponse();
            }
            catch (WebException)
            {
                return new CSteamID(0);
            }

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    JObject data = JObject.Parse(reader.ReadToEnd());
                    ulong lender = ulong.Parse(data["response"]["lender_steamid"].ToString());
                    return (lender == 0) ? altId : new CSteamID(lender);
                }
            }

            return new CSteamID(0);
        }

        public static void GetMainSteamIdAsync(CSteamID altId, Action<CSteamID> cb)
        {
            Task.Run(() => cb(GetMainSteamId(altId)));
        }
    }
}
