using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GameWeather
{
    public class ChangeWeather : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ChangeWeather;

        public int weatherType { get; set; }

        public override void HandlePacketClient()
        {
            Weather.CurrentWeather = ((Weather.WeatherType)weatherType);
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
