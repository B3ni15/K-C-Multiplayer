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
            IsReady = !player.ready;
            //SendToAll(KCClient.client.Id);

            player.ready = IsReady;
        }

        public override void HandlePacketClient()
        {
            player.ready = IsReady;
        }
    }
}
