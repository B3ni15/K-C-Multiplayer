using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game.Dragon
{
    public class SpawnMamaDragonPacket : Packet
    {
        public override ushort packetId => (int)Enums.Packets.SpawnMamaDragon;

        public Vector3 start { get; set; }

        public override void HandlePacketClient()
        {
            DragonSpawn.inst.SpawnMamaDragon(start);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
