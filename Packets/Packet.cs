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
                KCPlayer p = null;

                if (!Main.clientSteamIds.ContainsKey(clientId))
                    return null;

                //Main.helper.Log($"SteamID: {Main.GetPlayerByClientID(clientId).steamId} for {clientId} ({Main.GetPlayerByClientID(clientId).id})");

                if (Main.kCPlayers.TryGetValue(Main.GetPlayerByClientID(clientId).steamId, out p))
                    return p;
                else
                {
                    Main.helper.Log($"Error getting player from packet {packetId} {this.GetType().Name} from {clientId}");
                }

                return null;
            }
        }

        public void SendToAll(ushort exceptToClient = 0)
        {
            try
            {
                Main.LogSync($"SEND TO ALL: {this.GetType().Name} (id={packetId}) except={exceptToClient}");
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
                Main.LogSync($"SEND: {this.GetType().Name} (id={packetId}) to={toClient} myId={KCClient.client?.Id}");
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
