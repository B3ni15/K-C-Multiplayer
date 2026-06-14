using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game
{
    public class SetSpeed : Packet
    {
        public override ushort packetId => (int)Enums.Packets.SetSpeed;

        public int speed { get; set; }

        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) // Prevent speed softlock
                return;

            // Flag the change as remote-originated so the SetSpeed Harmony postfix
            // applies it locally without rebroadcasting it (avoids a feedback loop).
            Main.SpeedControlUISetSpeedHook.applyingRemoteSpeed = true;
            try
            {
                SpeedControlUI.inst.SetSpeed(speed);
            }
            finally
            {
                Main.SpeedControlUISetSpeedHook.applyingRemoteSpeed = false;
            }
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
