using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets
{
    public interface IPacket
    {
        ushort packetId { get; }
        ushort clientId { get; set; }

        void HandlePacketServer();
        void HandlePacketClient();
    }
}
