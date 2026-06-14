using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets.Game.GamePlayer
{
    // Syncs scalar per-player state (currently the simulation year) to the mirror
    // copies of a player that live on the other clients. Remote "Client Player"
    // instances never run their own Update, so without this their CurrYear stayed
    // stuck at 0 forever. Sent from the local player's StateObserver when a
    // monitored value changes; see Main.OnPlayerStateSendUpdate.
    public class PlayerStateUpdate : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.PlayerStateUpdate;

        public int currYear { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                if (KCClient.client.Id == clientId) return;

                var p = player;
                if (p == null || p.inst == null)
                    return;

                p.inst.CurrYear = currYear;
            }
            catch (Exception e)
            {
                Main.helper.Log("Error handling player state update packet: " + e.Message);
            }
        }

        public override void HandlePacketServer()
        {
        }
    }
}
