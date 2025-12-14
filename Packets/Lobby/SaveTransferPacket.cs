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
            if (chunkId == 0)
            {
                Main.helper.Log("Save Transfer started! Resetting static transfer state.");
                loadingSave = true;

                saveData = new byte[saveSize];
                chunksReceived = new bool[totalChunks];
                received = 0;

                ServerLobbyScript.LoadingSave.SetActive(true);
            }


            Array.Copy(saveDataChunk, 0, saveData, saveDataIndex, saveDataChunk.Length);

            chunksReceived[chunkId] = true;

            received += chunkSize;

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



            if (chunkId + 1 == totalChunks)
            {
                Main.helper.Log($"Received last save transfer packet.");

                Main.helper.Log(WhichIsNotComplete());
            }

            if (IsTransferComplete())
            {
                Main.helper.Log("Save Transfer complete!");

                LoadSaveLoadHook.saveBytes = saveData;
                LoadSaveLoadHook.memoryStreamHook = true;

                LoadSave.Load();

                GameState.inst.SetNewMode(GameState.inst.playingMode);
                LobbyManager.loadingSave = false;

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
