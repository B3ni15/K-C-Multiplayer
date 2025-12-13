using KCM.Packets.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KCM.Packets
{
    public abstract class Packet : IPacket
    {
        public abstract ushort packetId { get; }
        public ushort clientId { get; set; }

        public KCPlayer player
        {
            get
            {
                string steamId;
                if (!Main.clientSteamIds.TryGetValue(clientId, out steamId) || string.IsNullOrEmpty(steamId))
                    return null;

                KCPlayer player;
                if (Main.kCPlayers.TryGetValue(steamId, out player))
                    return player;

                Main.helper.Log($"Error getting player from packet {packetId} {GetType().Name} from {clientId}");
                return null;
            }
        }

        public void SendToAll(ushort exceptToClient = 0)
        {
            try
            {
                if (exceptToClient == 0)
                {
                    if (KCServer.IsRunning)
                        KCServer.server.SendToAll(PacketHandler.SerialisePacket(this));
                }
                else
                {
                    if (KCServer.IsRunning && exceptToClient != 0)
                        KCServer.server.SendToAll(PacketHandler.SerialisePacket(this), exceptToClient);
                }
            }
            catch (Exception ex)
            {
                Main.helper.Log($"Error sending packet to all {packetId} {this.GetType().Name} from {clientId}");

                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Main.helper.Log("----------------------- Inner exception -----------------------");
                    Main.helper.Log(ex.InnerException.ToString());
                    Main.helper.Log("----------------------- Inner message -----------------------");
                    Main.helper.Log(ex.InnerException.Message);
                    Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                    Main.helper.Log(ex.InnerException.StackTrace);
                }
            }
        }

        public void Send(ushort toClient = 0)
        {
            try
            {
                if (KCClient.client.IsConnected && toClient == 0)
                {
                    this.clientId = KCClient.client.Id;
                    KCClient.client.Send(PacketHandler.SerialisePacket(this));
                }
                else if (KCServer.IsRunning && toClient != 0)
                {
                    KCServer.server.Send(PacketHandler.SerialisePacket(this), toClient);
                }
            }
            catch (Exception ex)
            {
                Main.helper.Log($"Error sending packet {packetId} {this.GetType().Name} from {clientId}");

                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Main.helper.Log("----------------------- Inner exception -----------------------");
                    Main.helper.Log(ex.InnerException.ToString());
                    Main.helper.Log("----------------------- Inner message -----------------------");
                    Main.helper.Log(ex.InnerException.Message);
                    Main.helper.Log("----------------------- Inner stacktrace -----------------------");
                    Main.helper.Log(ex.InnerException.StackTrace);
                }
            }
        }

        public abstract void HandlePacketServer();
        public abstract void HandlePacketClient();
    }
}
