using KCM.Packets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Lobby
{
    public class WorldSeed : Packet
    {
        public override ushort packetId => (int)Enums.Packets.WorldSeed;
        public int Seed { get; set; }

        public override void HandlePacketServer()
        {
            //SetWorldSeed();
        }

        public override void HandlePacketClient()
        {
            SetWorldSeed();
        }

        public void SetWorldSeed()
        {
            try
            {
                foreach (var player in Main.kCPlayers.Values)
                    player.inst.Reset();

                World.inst.Generate(Seed);
                Vector3 center = World.inst.GetCellData(World.inst.GridWidth / 2, World.inst.GridHeight / 2).Center;
                Cam.inst.SetTrackingPos(center);
            }
            catch (Exception e)
            {
                Main.helper.Log("Set world seed packet error");
                Main.helper.Log(e.Message);
                Main.helper.Log(e.StackTrace);
                Main.helper.Log(e.ToString());
            }
        }
    }
}
