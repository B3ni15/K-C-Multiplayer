using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class PacketHandlerAttribute : Attribute
    {
        public ushort packetId;

        public PacketHandlerAttribute(ushort packetId)
        {
            this.packetId = packetId;
        }
    }
}
