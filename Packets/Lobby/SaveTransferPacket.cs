using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KCM.StateManagement.Observers;
using static KCM.Main;

namespace KCM.Packets.Lobby
{
    public class SaveTransferPacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.SaveTransferPacket;

        public static byte[] saveData = new byte[1];
        public static bool[] chunksReceived = new bool[1];
        public static bool loadingSave = false;
        public static int received = 0;

        public static void ResetTransferState()
        {
            loadingSave = false;
            received = 0;
            saveData = new byte[1];
            chunksReceived = new bool[1];
        }


        public int chunkId { get; set; }
        public int chunkSize { get; set; }

        public int saveSize { get; set; }
        public int saveDataIndex { get; set; }
        public int totalChunks { get; set; }

        public byte[] saveDataChunk { get; set; }

        public override void HandlePacketClient()
        {
            bool initialisingTransfer = !loadingSave ||
                                       saveData == null ||
                                       saveData.Length != saveSize ||
                                       chunksReceived == null ||
                                       chunksReceived.Length != totalChunks;

            if (initialisingTransfer)
            {
                Main.helper.Log("Save Transfer started!");
                loadingSave = true;
                received = 0;

                StateObserver.ClearAll();

                saveData = new byte[saveSize];
                chunksReceived = new bool[totalChunks];

                if (ServerLobbyScript.LoadingSave != null)
                    ServerLobbyScript.LoadingSave.SetActive(true);
            }

            if (chunkId < 0 || chunkId >= totalChunks)
            {
                Main.helper.Log($"Invalid save chunk id: {chunkId} / {totalChunks}");
                return;
            }

            if (saveDataChunk == null)
            {
                Main.helper.Log($"Null save chunk data for chunk: {chunkId}");
                return;
            }

            if (saveDataIndex < 0 || saveDataIndex + saveDataChunk.Length > saveData.Length)
            {
                Main.helper.Log($"Invalid save chunk write range: index={saveDataIndex} len={saveDataChunk.Length} size={saveData.Length}");
                return;
            }

            Array.Copy(saveDataChunk, 0, saveData, saveDataIndex, saveDataChunk.Length);
            chunksReceived[chunkId] = true;
            received += chunkSize;

            float savePercent = saveSize > 0 ? (float)received / (float)saveSize : 0f;
            if (ServerLobbyScript.ProgressBar != null)
                ServerLobbyScript.ProgressBar.fillAmount = savePercent;
            if (ServerLobbyScript.ProgressBarText != null)
                ServerLobbyScript.ProgressBarText.text = (savePercent * 100).ToString("0.00") + "%";
            if (ServerLobbyScript.ProgressText != null)
                ServerLobbyScript.ProgressText.text = $"{((float)(received / 1000)).ToString("0.00")} KB / {((float)(saveSize / 1000)).ToString("0.00")} KB";


            if (chunkId + 1 == totalChunks)
            {
                Main.helper.Log($"Received last save transfer packet.");

                Main.helper.Log(WhichIsNotComplete());
            }

            // Check if all chunks have been received
            if (IsTransferComplete())
            {
                // Handle completed transfer here
                Main.helper.Log("Save Transfer complete!");

                LoadSaveLoadHook.saveBytes = saveData;
                LoadSaveLoadHook.memoryStreamHook = true;

                LoadSave.Load();

                try
                {
                    Main.SetMultiplayerSaveLoadInProgress(true);
                    LoadSaveLoadHook.saveContainer.Unpack(null);
                }
                finally
                {
                    Main.SetMultiplayerSaveLoadInProgress(false);
                }
                Broadcast.OnLoadedEvent.Broadcast(new OnLoadedEvent());

                try
                {
                    new KCM.Packets.Network.ResyncRequestPacket { reason = "post-load" }.Send();
                }
                catch
                {
                }

                if (ServerLobbyScript.LoadingSave != null)
                    ServerLobbyScript.LoadingSave.SetActive(false);

                loadingSave = false;
                received = 0;
                saveData = new byte[1];
                chunksReceived = new bool[1];
            }
        }

        public static bool IsTransferComplete()
        {
            return chunksReceived.All(x => x == true);
        }

        public static string WhichIsNotComplete()
        {
            string notComplete = "";
            for (int i = 0; i < chunksReceived.Length; i++)
            {
                if (!chunksReceived[i])
                {
                    notComplete += i + ", ";
                }
            }
            return notComplete;
        }

        public override void HandlePacketServer()
        {
        }
    }
}
