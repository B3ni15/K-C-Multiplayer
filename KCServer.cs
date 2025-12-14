using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Riptide;
using Harmony;
using System.Reflection;
using KCM.Packets.Handlers;
using KCM.Packets.Lobby;
using KCM.ServerLobby;
using KCM.Packets;
using KCM.Packets.Network;
using Riptide.Demos.Steam.PlayerHosted;

namespace KCM
{
    public class KCServer : MonoBehaviour
    {
        public static Server server = null;
        public static bool started = false;

        public static void StartServer()
        {
            // Stop and cleanup existing server if running
            if (server != null)
            {
                if (server.IsRunning)
                {
                    server.Stop();
                }
                // Unsubscribe old event handlers to prevent memory leaks
                server.MessageReceived -= PacketHandler.HandlePacketServer;
            }

            server = new Server(Main.steamServer);
            server.MessageReceived += PacketHandler.HandlePacketServer;

            server.Start(0, 25, useMessageHandlers: false);

            server.ClientConnected += (obj, ev) =>
            {
                Main.helper.Log("Client connected");

                if (server.ClientCount > LobbyHandler.ServerSettings.MaxPlayers)
                {
                    ShowModal showModal = new ShowModal() { title = "Failed to connect", message = "Server is full." };

                    showModal.Send(ev.Client.Id);

                    server.DisconnectClient(ev.Client.Id); //, PacketHandler.SerialisePacket(showModal)
                    return;
                }

                ev.Client.CanQualityDisconnect = false;

                Main.helper.Log("Client ID is: " + ev.Client.Id);

                new ServerHandshake() { clientId = ev.Client.Id, loadingSave = LobbyManager.loadingSave }.Send(ev.Client.Id);
            };

            server.ClientDisconnected += (obj, ev) =>
            {
                new ChatSystemMessage()
                {
                    Message = $"{Main.GetPlayerByClientID(ev.Client.Id).name} has left the server.",
                }.SendToAll();

                Main.kCPlayers.Remove(Main.GetPlayerByClientID(ev.Client.Id).steamId);
                Destroy(LobbyHandler.playerEntries.Select(x => x.GetComponent<PlayerEntryScript>()).Where(x => x.Client == ev.Client.Id).FirstOrDefault().gameObject);

                Main.helper.Log($"Client disconnected. {ev.Reason}");
            };

            Main.helper.Log($"Listening on port 7777. Max {LobbyHandler.ServerSettings.MaxPlayers} clients.");


            //Main.kCPlayers.Add(1, new KCPlayer(1, Player.inst));

            //Player.inst = Main.GetPlayer();
        }

        /*[MessageHandler(25)]
        public static void ClientJoined(ushort id, Message message)
        {
            var name = message.GetString();

            Main.helper.Log(id.ToString());
            Main.helper.Log($"User connected: {name}");

            if (id == 1)
            {
                players.Add(id, new KCPlayer(name, id, Player.inst));
            }
            else
            {
                players.Add(id, new KCPlayer(name, id));
            }
        }*/

        public static bool IsRunning { get { return server != null && server.IsRunning; } }

        private void Update()
        {
            if (server != null)
                server.Update();
        }

        private void OnApplicationQuit()
        {
            if (server != null && server.IsRunning)
            {
                new ShowModal
                {
                    title = "Host disconnected",
                    message = "The host has left the game."
                }.SendToAll();

                server.Stop();
            }
        }

        private void Preload(KCModHelper helper)
        {
            helper.Log("server?");

            helper.Log("Preload run in server");
        }

        private void SceneLoaded(KCModHelper helper)
        {
        }
    }
}
