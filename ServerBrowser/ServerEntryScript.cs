using KCM.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KCM
{
    public class ServerEntryScript : MonoBehaviour
    {
        public int PlayerCount { get; set; }
        public DateTime Heartbeat { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int MaxPlayers { get; set; }
        public bool Locked { get; set; }
        public string Difficulty { get; set; }
        public string PlayerId { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<object> Permissions { get; set; }
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }


        public void Start()
        {
            transform.Find("Panel/ServerName").GetComponent<TextMeshProUGUI>().text = Name;
            transform.Find("Panel/ServerHost").GetComponent<TextMeshProUGUI>().text = Host;
            transform.Find("ServerDifficulty").GetComponent<TextMeshProUGUI>().text = Difficulty;

            transform.Find("ServerLocked").gameObject.SetActive(Locked);
            transform.Find("ServerPlayers").GetComponent<TextMeshProUGUI>().text = $"{PlayerCount}/{MaxPlayers}";

            transform.Find("Join").GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    string joiningId = (Environment.MachineName == "DESKTOP-ILSB1VC" && PlayerId == "76561198327621045") ? "127.0.0.1" : PlayerId;
                    Main.helper.Log($"Joining id is: " + joiningId);
                    KCClient.client.Connect(joiningId, useMessageHandlers: false);

                    Main.helper.Log("Set lobby script after connecting");

                    var lobbyScript = ServerBrowser.serverLobbyRef.GetComponent<ServerLobbyScript>();
                    if (lobbyScript == null)
                        lobbyScript = ServerBrowser.serverLobbyRef.AddComponent<ServerLobbyScript>();

                    lobbyScript.SetDetails(this);

                    ModalManager.ShowModal("Connecting to server", "Please wait while we connect to the server", "", false);
                }
                catch (Exception ex)
                {
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
            });
        }
    }
}
