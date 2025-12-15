using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Riptide.Demos.Steam.PlayerHosted;
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


        public int chunkId { get; set; }
        public int chunkSize { get; set; }

        public int saveSize { get; set; }
        public int saveDataIndex { get; set; }
        public int totalChunks { get; set; }

        public byte[] saveDataChunk { get; set; }

        public override void HandlePacketClient()
        {
            // Initialize on first chunk OR if arrays aren't properly sized yet
            // This handles out-of-order packet delivery
            if (!loadingSave || saveData.Length != saveSize || chunksReceived.Length != totalChunks)
            {
                Main.helper.Log($"Save Transfer initializing. saveSize={saveSize}, totalChunks={totalChunks}");
                loadingSave = true;

                saveData = new byte[saveSize];
                chunksReceived = new bool[totalChunks];
                received = 0;

                ServerLobbyScript.LoadingSave.SetActive(true);
            }

            // Skip if we already received this chunk (duplicate packet)
            if (chunksReceived[chunkId])
            {
                Main.helper.Log($"[SaveTransfer] Duplicate chunk {chunkId} received, skipping.");
                return;
            }

            Array.Copy(saveDataChunk, 0, saveData, saveDataIndex, saveDataChunk.Length);

            chunksReceived[chunkId] = true;

            received += chunkSize;

            Main.helper.Log($"[SaveTransfer] Processed chunk {chunkId}/{totalChunks}. Received: {received} bytes of {saveSize}.");

            // Update progress bar
            if (saveSize > 0)
            {
                float savePercent = (float)received / (float)saveSize;
                string receivedKB = ((float)received / 1000f).ToString("0.00");
                string totalKB = ((float)saveSize / 1000f).ToString("0.00");

                ServerLobbyScript.ProgressBar.fillAmount = savePercent;
                ServerLobbyScript.ProgressBarText.text = (savePercent * 100).ToString("0.00") + "%";
                ServerLobbyScript.ProgressText.text = $"{receivedKB} KB / {totalKB} KB";
            }
            else
            {
                ServerLobbyScript.ProgressBar.fillAmount = 0f;
                ServerLobbyScript.ProgressBarText.text = "0.00%";
                ServerLobbyScript.ProgressText.text = "0.00 KB / 0.00 KB";
            }

            if (IsTransferComplete())
            {
                Main.helper.Log("Save Transfer complete!");

                // Reset the loading state before processing
                loadingSave = false;

                LoadSaveLoadHook.saveBytes = saveData;
                LoadSaveLoadHook.memoryStreamHook = true;

                LoadSave.Load();

                GameState.inst.SetNewMode(GameState.inst.playingMode);
                LobbyManager.loadingSave = false;

                Broadcast.OnLoadedEvent.Broadcast(new OnLoadedEvent());

                ServerLobbyScript.LoadingSave.SetActive(false);

                // Reset static state for next transfer
                ResetTransferState();
            }
        }

        public static void ResetTransferState()
        {
            saveData = new byte[1];
            chunksReceived = new bool[1];
            loadingSave = false;
            received = 0;
        }

        public static bool IsTransferComplete()
        {
            return chunksReceived.All(x => x == true);
        }

        public override void HandlePacketServer()
        {
        }
    }
}
