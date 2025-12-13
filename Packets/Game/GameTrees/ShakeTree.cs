using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameTrees
{
    public class ShakeTree : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ShakeTree;

        public int idx { get; set; }

        public override void HandlePacketClient()
        {
            TreeSystem.inst.ShakeTree(idx);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
