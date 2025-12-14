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
        public Vector3 position { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                if (KCClient.client.Id == clientId) return;

                // Check for duplicate villager by guid
                var existingVillager = player.inst.Workers.data.FirstOrDefault(w => w != null && w.guid == guid);
                if (existingVillager != null)
                {
                    Main.helper.Log($"Villager with guid {guid} already exists, skipping duplicate");
                    return;
                }

                Main.helper.Log("Received add villager packet from " + player.name + $"({player.id})");

                Villager newVillager = Villager.CreateVillager();
                newVillager.guid = guid;

                // Set villager position
                if (position != Vector3.zero)
                {
                    newVillager.TeleportTo(position);
                }

                player.inst.Workers.Add(newVillager);
                player.inst.Homeless.Add(newVillager);

            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling add villager packet: " + e.Message);
                Main.helper.Log(e.StackTrace);
            }
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
