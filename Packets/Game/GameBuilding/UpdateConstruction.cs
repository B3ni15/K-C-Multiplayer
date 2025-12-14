using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameBuilding
{
    public class UpdateConstruction : Packet
    {
        public override ushort packetId => (int)Enums.Packets.UpdateConstruction;

        public Guid buildingId { get; set; }
        public float constructionProgress { get; set; }

        public override void HandlePacketClient()
        {
            if (KCClient.client.Id == clientId) return;

            //Main.helper.Log($"Received packet from: {clientId} receiving client is {KCClient.client.Id}");
            Player.inst.GetBuilding(buildingId).constructionProgress = constructionProgress;
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
