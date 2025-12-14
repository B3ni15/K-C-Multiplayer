using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game.GameVillager
{
    public class VillagerTeleportTo : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.VillagerTeleportTo;

        public Guid guid { get; set; }
        public Vector3 pos { get; set; }

        public override void HandlePacketClient()
        {
            if (KCClient.client.Id == clientId) return;

            try
            {
                Villager.villagers.data.Where(x => x.guid == guid).FirstOrDefault().TeleportTo(pos);

                Main.helper.Log($"Teleporting villager to {pos.ToString()}");
            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling villager teleport packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {

        }
    }
}
