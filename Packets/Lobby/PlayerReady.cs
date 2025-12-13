using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class PlayerReady : Packet
    {
        public override ushort packetId => (int)Enums.Packets.PlayerReady;

        public bool IsReady { get; set; }

        public override void HandlePacketServer()
        {
            if (player == null)
                return;
            IsReady = !player.ready;
            //SendToAll(KCClient.client.Id);

            player.ready = IsReady;
        }

        public override void HandlePacketClient()
        {
            if (player == null)
                return;
            player.ready = IsReady;
        }
    }
}
