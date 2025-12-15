using Harmony;
using KCM.Enums;
using KCM.Packets.Handlers;
using Newtonsoft.Json;
using Riptide.Demos.Steam.PlayerHosted;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace KCM
{
    public class ServerBrowser : MonoBehaviour
    {
        public static GameObject serverBrowserRef = null;
        public static Transform serverBrowserContentRef = null;

        public static GameObject serverLobbyRef = null;
        public static Transform serverLobbyPlayerRef = null;
        public static Transform serverLobbyChatRef = null;

        public static List<GameObject> ServerEntries = new List<GameObject>();
        public static ServerResponse ServerResponse = new ServerResponse();

        private string databaseId = "6563181402855cfc8b87"; // Replace with your database ID
        private string collectionId = "servers"; // Replace with your collection ID
        private string projectId = "kcmmasterserver"; // Replace with your project ID
        string apiKey = "f80c8f7f5c07a4d4600a7d9954529a8a7897de58c08d9c2b24eaf638dd66e7007917840cfeea5d2673ad397336b9d68ca48375ca6e918c41ddfbdb84a96fa009e9976dacfbaa0a3a8effd79f862f1ea249822e17d26e111c5da48e20ceb0065421fc7fca7e630172a003cc89dd00c5a636b443bc7c8d85149384db9d6d5f6df6"; // Replace with your API key

        private string serverID = string.Empty;

        public static GameObject inst { get; private set; }
        public void Awake()
        {
            inst = serverBrowserRef;
        }

        void Start()
        {
            inst = serverBrowserRef;
            StartCoroutine(LobbyHeartbeat());
        }

        public static bool registerServer = false;
        int interval = 0;

        IEnumerator LobbyHeartbeat()
        {
            while (true)
            {
                string url = $"https://base.ryanpalmer.tech/v1/databases/{databaseId}/collections/{collectionId}/documents";

                #region "Get Servers (for browser)"
                if (serverBrowserRef != null)
                {
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "GET";
                    request.Headers["X-Appwrite-Project"] = projectId;
                    request.Headers["X-Appwrite-Key"] = apiKey;

                    Task task = Task.Run(async () =>
                    {
                        using (WebResponse response = await request.GetResponseAsync())
                        {
                            using (Stream stream = response.GetResponseStream())
                            {
                                try
                                {
                                    StreamReader reader = new StreamReader(stream);
                                    string responseText = reader.ReadToEnd();

                                    ServerResponse serverResponse = JsonConvert.DeserializeObject<ServerResponse>(responseText);

                                    ServerResponse = serverResponse;

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
                            }
                        }
                    });


                    yield return new WaitUntil(() => task.IsCompleted);

                    DestroyServerEntries();

                    foreach (ServerEntry serverEntry in ServerResponse.Documents)
                    {
                        GameObject entry = Instantiate(PrefabManager.serverEntryItemPrefab, serverBrowserContentRef);
                        var s = entry.AddComponent<ServerEntryScript>();

                        s.Name = serverEntry.Name;
                        s.Host = serverEntry.Host;
                        s.MaxPlayers = serverEntry.MaxPlayers;
                        s.Locked = serverEntry.Locked;
                        s.PlayerCount = serverEntry.PlayerCount;
                        s.Difficulty = serverEntry.Difficulty;
                        s.Port = serverEntry.Port;
                        s.IPAddress = serverEntry.IPAddress;
                        s.PlayerId = serverEntry.PlayerId;

                        ServerEntries.Add(entry);
                    }
                }
                #endregion

                #region "Register Server"
                if (registerServer)
                {
                    //Main.helper.Log("Register server");
                    registerServer = false;

                    Task<WebResponse> registerTask = Task.Run(() =>
                    {
                        WebRequest request = WebRequest.Create(url);
                        request.Method = "POST";
                        request.ContentType = "application/json";
                        request.Headers["X-Appwrite-Project"] = projectId;
                        request.Headers["X-Appwrite-Key"] = apiKey;

                        serverID = SteamUser.GetSteamID().ToString();

                        string postData = JsonConvert.SerializeObject(new
                        {
                            documentId = serverID,
                            data = new
                            {
                                Name = LobbyHandler.ServerSettings.ServerName,
                                PlayerId = serverID,
                                Host = KCClient.inst.Name,
                                PlayerCount = KCServer.server.ClientCount,
                                MaxPlayers = LobbyHandler.ServerSettings.MaxPlayers,
                                Difficulty = Enum.GetName(typeof(Difficulty), LobbyHandler.ServerSettings.Difficulty),
                                Heartbeat = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                                //IPAddress = "127.0.0.1",
                                Port = 7777,
                                Locked = false
                            }
                        });

                        Main.helper.Log(postData);

                        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                        {
                            streamWriter.Write(postData);
                        }

                        return request.GetResponse();
                    });

                    // Wait until the task is completed
                    yield return new WaitUntil(() => registerTask.IsCompleted);

                    if (registerTask.Exception != null)
                    {
                        Main.helper.Log("Register error");
                        Main.helper.Log($"Task Exception: {registerTask.Exception}");
                        Main.helper.Log($"Task InnerException: {registerTask.Exception.InnerException}");
                        using (WebResponse response = registerTask.Result)
                        {
                            using (Stream dataStream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(dataStream))
                                {
                                    string responseFromServer = reader.ReadToEnd();
                                    //Main.helper.Log(responseFromServer);
                                }
                            }
                        }
                    }
                    else
                    {
                        using (WebResponse response = registerTask.Result)
                        {
                            using (Stream dataStream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(dataStream))
                                {
                                    string responseFromServer = reader.ReadToEnd();
                                    //Main.helper.Log(responseFromServer);
                                }
                            }
                        }
                    }
                }
                #endregion

                #region "Heartbeat"
                if (interval >= 8 && KCServer.IsRunning)
                {
                    //Main.helper.Log("Commence heartbeat");
                    Task<WebResponse> heartbeatTask = Task.Run(() =>
                    {
                        WebRequest request = WebRequest.Create(url + "/" + serverID);
                        request.Method = "PATCH";
                        request.ContentType = "application/json";
                        request.Headers["X-Appwrite-Project"] = projectId;
                        request.Headers["X-Appwrite-Key"] = apiKey;

                        // Create the request body
                        string postData = JsonConvert.SerializeObject(new
                        {
                            data = new
                            {
                                Name = LobbyHandler.ServerSettings.ServerName,
                                Host = KCClient.inst.Name,
                                PlayerCount = KCServer.server.ClientCount,
                                MaxPlayers = LobbyHandler.ServerSettings.MaxPlayers,
                                Difficulty = Enum.GetName(typeof(Difficulty), LobbyHandler.ServerSettings.Difficulty),
                                Heartbeat = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                                //IPAddress = "127.0.0.1",
                                Locked = LobbyHandler.ServerSettings.Locked
                            }
                        });

                        Main.helper.Log(postData);

                        using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                        {
                            streamWriter.Write(postData);
                        }

                        return request.GetResponse();
                    });

                    // Wait until the task is completed
                    yield return new WaitUntil(() => heartbeatTask.IsCompleted);

                    if (heartbeatTask.Exception != null)
                    {
                        Main.helper.Log("Heartbeat error");
                        Main.helper.Log($"Task Exception: {heartbeatTask.Exception.InnerException}");
                    }
                    else
                    {
                        using (WebResponse response = heartbeatTask.Result)
                        {
                            using (Stream dataStream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(dataStream))
                                {
                                    string responseFromServer = reader.ReadToEnd();
                                    //Main.helper.Log(responseFromServer);
                                }
                            }
                        }
                    }
                    //Main.helper.Log("Master server heartbeat");
                    interval = 0;
                }
                interval += 1;
                #endregion

                yield return new WaitForSecondsRealtime(2.0f);
            }
        }

        public static void DestroyServerEntries()
        {
            foreach (GameObject entry in ServerEntries)
                Destroy(entry);

            ServerEntries.Clear();
        }

        public static Transform KCMUICanvas { get; set; }

        private void SceneLoaded(KCModHelper helper)
        {
            Main.helper.Log("Serverbrowser scene loaded");


            try
            {
                if (Constants.MainMenuUI_T == null)
                {
                    Main.helper.Log("MainMenuUI_T is null in ServerBrowser");
                    return;
                }

                var topLevelCanvas = Constants.MainMenuUI_T.Find("TopLevelUICanvas");
                if (topLevelCanvas == null)
                {
                    Main.helper.Log("TopLevelUICanvas not found in ServerBrowser");
                    return;
                }

                GameObject kcmUICanvas = Instantiate(topLevelCanvas.gameObject);

                for (int i = 0; i < kcmUICanvas.transform.childCount; i++)
                    Destroy(kcmUICanvas.transform.GetChild(i).gameObject);

                kcmUICanvas.name = "KCMUICanvas";
                kcmUICanvas.transform.SetParent(Constants.MainMenuUI_T);

                KCMUICanvas = kcmUICanvas.transform;

                serverBrowserRef = GameObject.Instantiate(PrefabManager.serverBrowserPrefab, KCMUICanvas.transform);
                serverBrowserRef.SetActive(false);
                serverBrowserContentRef = serverBrowserRef.transform.Find("Container/Scroll View/Viewport/Content");

                //hides player name prompt
                serverBrowserRef.transform.Find("Container/PlayerName").gameObject.SetActive(false);



                serverLobbyRef = GameObject.Instantiate(PrefabManager.serverLobbyPrefab, KCMUICanvas.transform);
                serverLobbyPlayerRef = serverLobbyRef.transform.Find("Container/PlayerList/Viewport/Content");
                serverLobbyChatRef = serverLobbyRef.transform.Find("Container/PlayerChat/Viewport/Content");
                serverLobbyRef.SetActive(false);
                //browser.transform.position = new Vector3(0, 0, 0);


                var lobbyScript = serverLobbyRef.GetComponent<ServerLobbyScript>();
                if (lobbyScript == null)
                    lobbyScript = serverLobbyRef.AddComponent<ServerLobbyScript>();


                Main.helper.Log($"{lobbyScript == null}");


                //Create Server
                serverBrowserRef.transform.Find("Container/Create").GetComponent<Button>().onClick.AddListener(() =>
                {
                    try
                    {
                        SfxSystem.PlayUiSelect();


                        //KCServer.StartServer();
                        Main.helper.Log((LobbyManager.Singleton == null).ToString());
                        LobbyManager.Singleton.CreateLobby();
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

                //Load Server
                serverBrowserRef.transform.Find("Container/Load").GetComponent<Button>().onClick.AddListener(() =>
                {
                    try
                    {
                        SfxSystem.PlayUiSelect();

                        LobbyManager.Singleton.CreateLobby(true);
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

                //Back to Main Menu
                serverBrowserRef.transform.Find("Container/Back").GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxSystem.PlayUiSelect();


                    Main.TransitionTo(MenuState.Menu);
                });


                //Back to server browser
                serverLobbyRef.transform.Find("Container/Back").GetComponent<Button>().onClick.AddListener(() =>
                {
                    SfxSystem.PlayUiSelect();


                    LobbyManager.Singleton.LeaveLobby();
                    LobbyManager.loadingSave = false;
                });

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
        }

        private void Preload(KCModHelper helper)
        {
            helper.Log("Hello?");
            try
            {

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

            helper.Log("Preload run in serverbrowser");
        }
    }

    public class ServerResponse
    {
        public int Total { get; set; }
        public List<ServerEntry> Documents { get; set; }
    }

    public class ServerEntry
    {
        public int PlayerCount { get; set; }
        public DateTime Heartbeat { get; set; }
        public string Difficulty { get; set; }
        public int Port { get; set; }
        public string IPAddress { get; set; }
        public string Name { get; set; }
        public string Host { get; set; }
        public int MaxPlayers { get; set; }
        public bool Locked { get; set; }
        public string PlayerId { get; set; }
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<object> Permissions { get; set; }
        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
    }
}
