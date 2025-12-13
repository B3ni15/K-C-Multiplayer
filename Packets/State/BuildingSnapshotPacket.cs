using KCM.StateManagement.Sync;
using System;

namespace KCM.Packets.State
{
    public class BuildingSnapshotPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.BuildingSnapshot;

        public byte[] payload { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                if (payload == null || payload.Length == 0)
                    return;

                SyncManager.ApplyBuildingSnapshot(payload);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error applying BuildingSnapshotPacket");
                Main.helper.Log(ex.ToString());
            }
        }

        public override void HandlePacketServer()
        {
        }
    }
}

