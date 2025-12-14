using KCM.Packets.Handlers;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class PlayerList : Packet
    {
        public override ushort packetId => (int)Enums.Packets.PlayerList;

        public List<bool> playersReady { get; set; }
        public List<string> playersName { get; set; }
        public List<int> playersBanner { get; set; }
        public List<ushort> playersId { get; set; }
        public List<string> playersKingdomName { get; set; }

        public List<string> steamIds { get; set; }

        public override void HandlePacketServer()
        {

        }

        public override void HandlePacketClient()
        {
            LobbyHandler.ClearPlayerList();

            for (int i = 0; i < playersId.Count; i++)
            {

                Main.helper.Log("PlayerList: " + playersName[i] + " " + playersId[i] + " " + steamIds[i]);

                Main.kCPlayers.Add(steamIds[i], new KCPlayer(playersName[i], playersId[i], steamIds[i])
                {
                    name = playersName[i],
                    ready = playersReady[i],
                    banner = playersBanner[i],
                    kingdomName = playersKingdomName[i]
                });


                if (Main.clientSteamIds.ContainsKey(playersId[i]))
                    Main.clientSteamIds[playersId[i]] = steamIds[i];
                else
                    Main.clientSteamIds.Add(playersId[i], steamIds[i]);

                Main.kCPlayers[steamIds[i]].inst.PlayerLandmassOwner.SetBannerIdx(playersBanner[i]);

                LobbyHandler.AddPlayerEntry(playersId[i]);
            }
        }
    }
}
