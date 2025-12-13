using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game.GamePlayer
{
    public class AddVillagerPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.AddVillager;

        public Guid guid { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                if (KCClient.client.Id == clientId) return;

                Main.helper.Log("Received add villager packet from " + player.name + $"({player.id})");

                Villager v = Villager.CreateVillager();
                v.guid = guid;

                player.inst.Workers.Add(v);
                player.inst.Homeless.Add(v);

            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling add villager packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
