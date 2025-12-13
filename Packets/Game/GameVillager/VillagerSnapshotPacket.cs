using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KCM.Packets.Game.GameVillager
{
    public class VillagerSnapshotPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.VillagerSnapshot;

        public List<Guid> guids { get; set; } = new List<Guid>();
        public List<Vector3> positions { get; set; } = new List<Vector3>();

        public override void HandlePacketClient()
        {
            if (KCClient.client != null && clientId == KCClient.client.Id)
                return;

            try
            {
                int count = Math.Min(guids?.Count ?? 0, positions?.Count ?? 0);
                if (count == 0)
                    return;

                for (int i = 0; i < count; i++)
                {
                    Guid guid = guids[i];
                    Vector3 position = positions[i];

                    Villager villager = null;
                    try
                    {
                        villager = Villager.villagers?.data.FirstOrDefault(v => v != null && v.guid == guid);
                    }
                    catch
                    {
                    }

                    if (villager == null)
                        continue;

                    villager.TeleportTo(position);
                }
            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling villager snapshot packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {
        }
    }
}
