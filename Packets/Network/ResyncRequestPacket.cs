using KCM.Attributes;
using KCM.StateManagement.Sync;
using System;

namespace KCM.Packets.Network
{
    [NoServerRelay]
    public class ResyncRequestPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.ResyncRequest;

        public string reason { get; set; }

        public override void HandlePacketClient()
        {
        }

        public override void HandlePacketServer()
        {
            try
            {
                if (!KCServer.IsRunning)
                    return;

                SyncManager.SendResyncToClient(clientId, reason);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error handling ResyncRequestPacket on server");
                Main.helper.Log(ex.ToString());
            }
        }
    }
}

