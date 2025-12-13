using KCM.StateManagement.Sync;
using System;
using System.Collections.Generic;

namespace KCM.Packets.State
{
    public class ResourceSnapshotPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.ResourceSnapshot;

        public override Riptide.MessageSendMode sendMode => Riptide.MessageSendMode.Unreliable;

        public List<int> resourceTypes { get; set; }
        public List<int> amounts { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                if (KCClient.client.IsConnected && KCClient.client.Id == clientId)
                    return;

                SyncManager.ApplyResourceSnapshot(resourceTypes, amounts);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error applying ResourceSnapshotPacket");
                Main.helper.Log(ex.ToString());
            }
        }

        public override void HandlePacketServer()
        {
        }
    }
}

