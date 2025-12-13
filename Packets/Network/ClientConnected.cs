using KCM.Packets.Handlers;
using KCM.Packets.Lobby;
using Riptide.Demos.Steam.PlayerHosted;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static KCM.Main;

namespace KCM.Packets.Network
{
    public class ClientConnected : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ClientConnected;

        public string Name { get; set; }

        public string SteamId { get; set; }

        public override void HandlePacketClient()
        {

            Main.helper.Log("Client Player Connected: " + Name + " Id: " + clientId + " SteamID: " + SteamId);

            KCPlayer player;
            if (Main.kCPlayers.TryGetValue(SteamId, out player))
            {
                player.id = clientId;
                player.name = Name;
                player.steamId = SteamId;
            }
            else
                Main.kCPlayers.Add(SteamId, new KCPlayer(Name, clientId, SteamId));


            if (Main.clientSteamIds.ContainsKey(clientId))
                Main.clientSteamIds[clientId] = SteamId;
            else
                Main.clientSteamIds.Add(clientId, SteamId);


            if (!SaveTransferPacket.loadingSave)
                LobbyHandler.AddPlayerEntry(clientId);
        }

        public override void HandlePacketServer()
        {
            Main.helper.Log("Server Player Connected: " + Name + " Id: " + clientId + " SteamID: " + SteamId);

            KCPlayer player;
            if (Main.kCPlayers.TryGetValue(SteamId, out player))
            {
                player.id = clientId;
                player.name = Name;
                player.steamId = SteamId;
            }
            else
            {
                Main.kCPlayers[SteamId] = new KCPlayer(Name, clientId, SteamId);
            }

            Main.clientSteamIds[clientId] = SteamId;

            List<KCPlayer> list = Main.kCPlayers.Select(x => x.Value).OrderBy(x => x.id).ToList();

            if (list.Count > 0)
                new PlayerList()
                {
                    playersBanner = list.Select(x => x.banner).ToList(),
                    playersReady = list.Select(x => x.ready).ToList(),
                    playersName = list.Select(x => x.name).ToList(),
                    playersId = list.Select(x => x.id).ToList(),
                    playersKingdomName = list.Select(x => x.kingdomName).ToList(),
                    steamIds = list.Select(x => x.steamId).ToList()
                }.SendToAll(KCClient.client.Id);

            new ChatSystemMessage()
            {
                Message = $"{Name} has joined the server."
            }.SendToAll();

            LobbyHandler.ServerSettings.SendToAll(KCClient.client.Id);


            if (LobbyManager.loadingSave)
            {
                if (clientId == KCClient.client.Id)
                    return;

                byte[] bytes = LoadSaveLoadAtPathHook.saveData;
                KCServer.EnqueueSaveTransfer(clientId, bytes);
            }
            else
            {

                new WorldSeed()
                {
                    Seed = World.inst.seed
                }.SendToAll(KCClient.client.Id);
            }
        }

        public static List<byte[]> SplitByteArrayIntoChunks(byte[] source, int chunkSize)
        {
            var chunks = new List<byte[]>();
            int sourceLength = source.Length;

            for (int i = 0; i < sourceLength; i += chunkSize)
            {
                // Calculate the length of the current chunk, as the last chunk may be smaller than chunkSize
                int currentChunkSize = Math.Min(chunkSize, sourceLength - i);

                // Create a chunk array of the correct size
                byte[] chunk = new byte[currentChunkSize];

                // Copy a segment of the source array into the chunk array
                Array.Copy(source, i, chunk, 0, currentChunkSize);

                // Add the chunk to the list of chunks
                chunks.Add(chunk);
            }

            return chunks;
        }
    }
}
