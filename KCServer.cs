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
        public static Server server = new Server(Main.steamServer);
        public static bool started = false;

        private static readonly Dictionary<ushort, Queue<SaveTransferPacket>> saveTransferQueues = new Dictionary<ushort, Queue<SaveTransferPacket>>();
        private const int SaveTransferPacketsPerUpdatePerClient = 10;

        static KCServer()
        {
            //server.registerMessageHandler(typeof(KCServer).GetMethod("ClientJoined"));

            server.MessageReceived += PacketHandler.HandlePacketServer;
        }

        public static void StartServer()
        {
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
                ev.Client.MaxSendAttempts = 50;

                Main.helper.Log("Client ID is: " + ev.Client.Id);

                new ServerHandshake() { clientId = ev.Client.Id, loadingSave = LobbyManager.loadingSave }.Send(ev.Client.Id);
            };

            server.ClientDisconnected += (obj, ev) =>
            {
                try
                {
                    var playerName = $"Client {ev.Client.Id}";
                    string steamId;
                    if (Main.clientSteamIds.TryGetValue(ev.Client.Id, out steamId) && !string.IsNullOrEmpty(steamId))
                    {
                        KCPlayer player;
                        if (Main.kCPlayers.TryGetValue(steamId, out player) && player != null && !string.IsNullOrEmpty(player.name))
                            playerName = player.name;

                        Main.kCPlayers.Remove(steamId);
                    }

                    Main.clientSteamIds.Remove(ev.Client.Id);

                    new ChatSystemMessage()
                    {
                        Message = $"{playerName} has left the server.",
                    }.SendToAll();

                    var entry = LobbyHandler.playerEntries
                        .Select(x => x != null ? x.GetComponent<PlayerEntryScript>() : null)
                        .FirstOrDefault(x => x != null && x.Client == ev.Client.Id);

                    if (entry != null)
                        Destroy(entry.gameObject);

                    saveTransferQueues.Remove(ev.Client.Id);

                    Main.helper.Log($"Client disconnected. {ev.Reason}");
                }
                catch (Exception ex)
                {
                    Main.helper.Log("Error handling client disconnect");
                    Main.helper.Log(ex.ToString());
                }
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

        public static bool IsRunning { get { return server.IsRunning; } }

        private void Update()
        {
            server.Update();
            ProcessSaveTransfers();
            KCM.StateManagement.Sync.SyncManager.ServerUpdate();
        }

        private static void ProcessSaveTransfers()
        {
            if (!KCServer.IsRunning)
                return;

            if (saveTransferQueues.Count == 0)
                return;

            var clients = saveTransferQueues.Keys.ToList();
            foreach (var clientId in clients)
            {
                Queue<SaveTransferPacket> queue;
                if (!saveTransferQueues.TryGetValue(clientId, out queue) || queue == null)
                    continue;

                int sentThisUpdate = 0;
                while (sentThisUpdate < SaveTransferPacketsPerUpdatePerClient && queue.Count > 0)
                {
                    var packet = queue.Dequeue();
                    packet.Send(clientId);
                    sentThisUpdate++;
                }

                if (queue.Count == 0)
                    saveTransferQueues.Remove(clientId);
            }
        }

        public static void EnqueueSaveTransfer(ushort toClient, byte[] bytes)
        {
            if (bytes == null)
                return;

            int chunkSize = 900;
            int sent = 0;
            int totalChunks = (int)Math.Ceiling((double)bytes.Length / chunkSize);

            var queue = new Queue<SaveTransferPacket>(totalChunks);
            for (int i = 0; i < totalChunks; i++)
            {
                int currentChunkSize = Math.Min(chunkSize, bytes.Length - sent);
                var chunk = new byte[currentChunkSize];
                Array.Copy(bytes, sent, chunk, 0, currentChunkSize);

                queue.Enqueue(new SaveTransferPacket()
                {
                    saveSize = bytes.Length,
                    saveDataChunk = chunk,
                    chunkId = i,
                    chunkSize = chunk.Length,
                    saveDataIndex = sent,
                    totalChunks = totalChunks
                });

                sent += currentChunkSize;
            }

            saveTransferQueues[toClient] = queue;
            Main.helper.Log($"Queued {totalChunks} save data chunks for client {toClient}");
        }

        private void OnApplicationQuit()
        {
            server.Stop();
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
