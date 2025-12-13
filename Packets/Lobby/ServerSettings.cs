using KCM.Packets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class ServerSettings : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ServerSettings;

        public string ServerName { get; set; }
        public int MaxPlayers { get; set; }
        public bool Locked { get; set; }
        public string Password { get; set; }
        public int Difficulty { get; set; }
        public string WorldSeed { get; set; }
        public World.MapSize WorldSize { get; set; }
        public World.MapBias WorldType { get; set; }
        public World.MapRiverLakes WorldRivers { get; set; }
        public int PlacementType { get; set; }
        public bool FogOfWar { get; set; }

        public ServerSettings() { this.MaxPlayers = 2; this.Password = " "; this.WorldRivers = World.MapRiverLakes.Some; }

        public override void HandlePacketServer()
        {
            //SetServerSettings();
        }

        public override void HandlePacketClient()
        {
            SetServerSettings();
        }

        public void SetServerSettings()
        {

            LobbyHandler.ServerSettings = this;

            World.inst.mapSize = WorldSize;
            World.inst.mapBias = WorldType;
            World.inst.mapRiverLakes = WorldRivers;
        }
    }
}
