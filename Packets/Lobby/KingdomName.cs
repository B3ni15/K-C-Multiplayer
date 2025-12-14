using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class KingdomName : Packet
    {
        public override ushort packetId => (int)Enums.Packets.KingdomName;

        public string kingdomName { get; set; }

        public override void HandlePacketServer()
        {
            if (player == null)
                return;
            Main.helper.Log("Received kingdom name packet");

            //SendToAll(KCClient.client.Id);

            player.kingdomName = kingdomName;
        }

        public override void HandlePacketClient()
        {
            if (player == null)
                return;

            Main.helper.Log("Received kingdom name packet");

            player.kingdomName = kingdomName;

            Main.helper.Log($"Player {player.name} has joined with their kingdom {player.kingdomName}");
        }
    }
}
