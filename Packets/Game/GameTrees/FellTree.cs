using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameTrees
{
    public class FellTree : Packet
    {
        public override ushort packetId => (int)Enums.Packets.FellTree;

        public int idx { get; set; }

        public int x { get; set; }
        public int z { get; set; }

        public override void HandlePacketClient()
        {
            Cell cell = World.inst.GetCellData(x, z);

            TreeSystem.inst.FellTree(cell, idx);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
