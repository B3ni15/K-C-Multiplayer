using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game
{
    public class SetSpeed : Packet
    {
        public override ushort packetId => (int)Enums.Packets.SetSpeed;

        public int speed { get; set; }
        public bool isPaused { get; set; }

        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) // Prevent speed softlock
                return;

            try
            {
                Main.helper.Log($"Received SetSpeed packet: speed={speed}, isPaused={isPaused}");

                // Simply apply the speed - SpeedControlUISetSpeedHook will handle this correctly
                // since we're coming from a packet (PacketHandler.IsHandlingPacket will be true)
                SpeedControlUI.inst.SetSpeed(speed);
            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling SetSpeed packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {
            // Server relay is handled automatically by PacketHandler unless [NoServerRelay] is used.
        }
    }
}
