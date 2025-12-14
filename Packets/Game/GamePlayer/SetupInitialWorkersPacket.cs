using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GamePlayer
{
    public class SetupInitialWorkersPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.SetupInitialWorkers;

        public Guid keepGuid { get; set; }


        public override void HandlePacketClient()
        {
            if (KCClient.client.Id == clientId) return;

            /*Keep keep = player.inst.GetBuilding(keepGuid).GetComponent<Keep>();
            if (keep == null)
            {
                Main.helper.Log("Keep not found.");
                return;
            }

            player.inst.SetupInitialWorkers(keep);*/
        }

        public override void HandlePacketServer()
        {

        }
    }
}
