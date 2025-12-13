using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameBuilding
{
    public class CompleteBuild : Packet
    {
        public override ushort packetId => (int)Enums.Packets.CompleteBuild;

        public Guid buildingId { get; set; }

        public override void HandlePacketClient()
        {
            if (KCClient.client.Id == clientId) return;

            Player.inst.GetBuilding(buildingId).CompleteBuild();
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
