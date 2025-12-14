using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameTrees
{
    public class GrowTree : Packet
    {
        public override ushort packetId => (int)Enums.Packets.GrowTree;

        public int X { get; set; }
        public int Z { get; set; }

        public override void HandlePacketClient()
        {
            Cell cell = World.inst.GetCellData(X, Z);

            TreeSystem.inst.GrowTree(cell);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
