using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            float savePercent = (float)received / (float)saveSize;

            // Initialize saveData and chunksReceived on the first packet received
            if (saveData.Length == 1)
            {

                Main.helper.Log("Save Transfer started!");
                loadingSave = true;

                ServerLobbyScript.LoadingSave.SetActive(true);

                // save percentage


                saveData = new byte[saveSize];
                chunksReceived = new bool[totalChunks];
            }


            // Copy the chunk data into the correct position in saveData
            Array.Copy(saveDataChunk, 0, saveData, saveDataIndex, saveDataChunk.Length);

            // Mark this chunk as received
            chunksReceived[chunkId] = true;

            // Seek to the next position to write to
            received += chunkSize;


            ServerLobbyScript.ProgressBar.fillAmount = savePercent;
            ServerLobbyScript.ProgressBarText.text = (savePercent * 100).ToString("0.00") + "%";
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


                LoadSaveLoadHook.saveContainer.Unpack(null);
                Broadcast.OnLoadedEvent.Broadcast(new OnLoadedEvent());

                ServerLobbyScript.LoadingSave.SetActive(false);
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
