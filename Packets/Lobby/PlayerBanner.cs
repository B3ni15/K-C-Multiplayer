using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Lobby
{
    public class PlayerBanner : Packet
    {
        public override ushort packetId => (int)Enums.Packets.PlayerBanner;

        public int banner { get; set; }

        public override void HandlePacketServer()
        {
            //SendToAll(KCClient.client.Id);

            //player.banner = banner;
            //player.inst.PlayerLandmassOwner.SetBannerIdx(banner);
        }

        public override void HandlePacketClient()
        {
            player.banner = banner;
            player.inst.PlayerLandmassOwner.SetBannerIdx(banner);

            Main.helper.Log($"Player {clientId} ({player.id}) has set banner to {player.banner}");
        }
    }
}
