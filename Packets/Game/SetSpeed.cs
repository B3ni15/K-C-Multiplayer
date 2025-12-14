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

            SpeedControlUI.inst.SetSpeed(speed);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
