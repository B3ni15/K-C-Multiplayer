using KCM.Packets.Game;
using KCM.Packets.Handlers;
using KCM.Packets.Lobby;
using Riptide.Demos.Steam.PlayerHosted;
using Steamworks;
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
    public class ServerLobbyScript : MonoBehaviour
    {
        public static ServerEntryScript serverDetails { get; set; }

        public static Transform PlayerListContent { get; set; }
        public static Transform PlayerChatContent { get; set; }

        public static Button StartGameButton { get; set; }

        public static TMP_InputField ChatInput { get; set; }
        public static Button ChatSendButton { get; set; }

        public static TMP_InputField ServerName { get; set; }

        public static TMP_InputField MaxPlayers { get; set; }
        public static Toggle Locked { get; set; }
        public static TMP_InputField Password { get; set; }

        public static TMP_Dropdown Difficulty { get; set; }

        public static TMP_InputField WorldSeed { get; set; }
        public static Button NewWorldButton { get; set; }

        public static TMP_Dropdown WorldSize { get; set; }
        public static TMP_Dropdown WorldType { get; set; }
        public static TMP_Dropdown WorldRivers { get; set; }

        public static TMP_Dropdown PlacementType { get; set; }
        public static Toggle FogOfWarToggle { get; set; }

        public static ServerLobbyScript inst { get; set; }


        public static GameObject LoadingSave { get; set; }
        public static Image ProgressBar { get; set; }
        public static TextMeshProUGUI ProgressBarText { get; set; }
        public static TextMeshProUGUI ProgressText { get; set; }

        enum Difficulties
        {
            Paxlon,
            Sommern,
            Vintar,
            Falle
        }

        bool awake = false;

        public void Start()
        {
            Main.helper.Log("ServerLobby start called");
            inst = this;
        }

        public void Awake()
        {
            Main.helper.Log("ServerLobby awake called");
            try
            {
                inst = this;
                PlayerListContent = transform.Find("Container/PlayerList/Viewport/Content");
                Main.helper.Log(PlayerListContent.ToString());

                PlayerChatContent = transform.Find("Container/PlayerChat/Viewport/Content");
                Main.helper.Log(PlayerChatContent.ToString());
                ChatInput = transform.Find("Container/TextMeshPro - InputField").GetComponent<TMP_InputField>();
                Main.helper.Log(ChatInput.ToString());
                ChatSendButton = transform.Find("Container/SendMessage").GetComponent<Button>();
                Main.helper.Log(ChatSendButton.ToString());


                StartGameButton = transform.Find("Container/Start").GetComponent<Button>();


                ServerName = transform.Find("Container/ServerSettings/ServerName").GetComponentInChildren<TMP_InputField>();
                ServerName.text = $"{SteamFriends.GetPersonaName()}'s Server";
                Main.helper.Log(ServerName.ToString());
                MaxPlayers = transform.Find("Container/ServerSettings/ServerAccess/MaxPlayers").GetComponentInChildren<TMP_InputField>();
                MaxPlayers.text = "2";
                Main.helper.Log(ServerName.ToString());
                Locked = transform.Find("Container/ServerSettings/ServerAccess/Toggle").GetComponent<Toggle>();
                Locked.isOn = false;
                Main.helper.Log(Locked.ToString());
                Password = transform.Find("Container/ServerSettings/ServerAccess/Password").GetComponentInChildren<TMP_InputField>();
                Main.helper.Log(Password.ToString());
                Password.text = " ";

                Difficulty = transform.Find("Container/ServerSettings/Difficulty").GetComponentInChildren<TMP_Dropdown>();
                Difficulty.value = 0;
                Main.helper.Log(Difficulty.ToString());

                WorldSeed = transform.Find("Container/ServerSettings/WorldSettings/SeedInput").GetComponent<TMP_InputField>();
                Main.helper.Log(WorldSeed.ToString());
                WorldSeed.text = " ";
                NewWorldButton = transform.Find("Container/ServerSettings/WorldSettings/NewMap").GetComponent<Button>();
                Main.helper.Log(ChatSendButton.ToString());

                WorldSize = transform.Find("Container/ServerSettings/WorldSettings/WorldSize").GetComponentInChildren<TMP_Dropdown>();
                Main.helper.Log(WorldSize.ToString());
                WorldType = transform.Find("Container/ServerSettings/WorldSettings/WorldType").GetComponentInChildren<TMP_Dropdown>();
                WorldType.value = 1;
                Main.helper.Log(WorldType.ToString());
                WorldRivers = transform.Find("Container/ServerSettings/WorldSettings/WorldRivers").GetComponentInChildren<TMP_Dropdown>();
                WorldRivers.value = 1;
                Main.helper.Log(WorldRivers.ToString());


                FogOfWarToggle = transform.Find("Container/ServerSettings/WorldSettings/FogOfWarToggle").GetComponent<Toggle>();
                FogOfWarToggle.isOn = true;

                PlacementType = transform.Find("Container/ServerSettings/WorldSettings/Placement").GetComponentInChildren<TMP_Dropdown>();
                PlacementType.value = 0;

                PlacementType.enabled = true;
                FogOfWarToggle.enabled = false;
                FogOfWarToggle.gameObject.SetActive(false);

                Main.helper.Log("Loading save parent");
                LoadingSave = transform.Find("LoadingSave").gameObject;
                Main.helper.Log("Loading save progress bar mask");
                ProgressBar = transform.Find("LoadingSave/Window/Progress Bar/Mask").GetComponent<Image>();
                Main.helper.Log("Loading save progress bar text");
                ProgressBarText = transform.Find("LoadingSave/Window/Progress Bar").GetComponentInChildren<TextMeshProUGUI>();
                Main.helper.Log("Loading save progress text");
                ProgressText = transform.Find("LoadingSave/Window/Information").GetComponent<TextMeshProUGUI>();

                if (!KCServer.IsRunning)
                {

                    Main.helper.Log("Disable all");
                    StartGameButton.onClick.RemoveAllListeners();
                    StartGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
                    StartGameButton.onClick.AddListener(() =>
                    {
                        new PlayerReady()
                        {
                            IsReady = true
                        }.Send();
                    });

                    ServerName.DeactivateInputField();
                    ServerName.enabled = false;
                    ServerName.interactable = false;
                    MaxPlayers.DeactivateInputField();
                    MaxPlayers.enabled = false;
                    MaxPlayers.interactable = false;

                    Locked.enabled = false;
                    Locked.interactable = false;
                    Password.DeactivateInputField();
                    Password.enabled = false;
                    Password.interactable = false;

                    Difficulty.enabled = false;
                    Difficulty.interactable = false;

                    WorldSeed.enabled = false;
                    WorldSeed.interactable = false;
                    NewWorldButton.enabled = false;
                    NewWorldButton.interactable = false;

                    WorldSize.enabled = false;
                    WorldSize.interactable = false;
                    WorldType.enabled = false;
                    WorldType.interactable = false;
                    WorldRivers.enabled = false;
                    WorldRivers.interactable = false;
                }
                else
                {
                    StartGameButton.onClick.RemoveAllListeners();
                    StartGameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
                    StartGameButton.onClick.AddListener(() =>
                    {
                        if (World.inst.GetTextSeed() != WorldSeed.text)
                        {
                            World.inst.seed = World.inst.SeedFromText(WorldSeed.text);
                        }

                        new WorldSeed()
                        {
                            Seed = World.inst.seed
                        }.SendToAll(KCClient.client.Id);

                        new StartGame().SendToAll();

                        if (PlacementType.value == 0 && !LobbyManager.loadingSave)
                        {
                            List<int> takenIdxs = new List<int>();
                            foreach (KCPlayer kcPlayer in Main.kCPlayers.Values)
                            {
                                var idx = SRand.Range(0, World.inst.NumLandMasses);
                                while (takenIdxs.Contains(idx))
                                {
                                    Main.helper.Log($"LANDMASS: {idx} IS ALREADY TAKEN");
                                    idx = SRand.Range(0, World.inst.NumLandMasses);
                                }
                                takenIdxs.Add(idx);

                                Main.helper.Log($"SENDING PLACE KEEP RANDOMLY FOR {kcPlayer.name} ON LANDMASS: {idx}");

                                new PlaceKeepRandomly()
                                {
                                    landmassIdx = idx
                                }.Send(kcPlayer.id);
                            }
                        }
                    });

                }


                ChatSendButton.onClick.AddListener(() =>
                {
                    if (ChatInput.text.Length > 0)
                    {
                        new ChatMessage()
                        {
                            PlayerName = KCClient.inst.Name,
                            Message = ChatInput.text
                        }.Send();

                        ChatInput.text = "";
                    }
                });

                NewWorldButton.onClick.AddListener(() =>
                {
                    if (KCServer.IsRunning)
                    {
                        try
                        {

                            foreach (var player in Main.kCPlayers.Values)
                                if (player.inst.PlayerLandmassOwner.teamId != Player.inst.PlayerLandmassOwner.teamId)
                                    player.inst.Reset();

                            Main.helper.Log("new world button");
                            World.inst.Generate();
                            WorldSeed.text = World.inst.GetTextSeed();

                            foreach (var player in Main.kCPlayers.Values)
                                player.inst.SetupJobPriorities();

                            new WorldSeed()
                            {
                                Seed = World.inst.seed
                            }.SendToAll(KCClient.client.Id);
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
                });

                SetValues();
                InvokeRepeating("SetValues", 0, 1f);
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

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                ChatSendButton.onClick.Invoke();
            }
        }

        public void FixedUpdate()
        {

        }

        public void SetValues()
        {
            try
            {
                if (!KCServer.IsRunning)
                {

                    ServerName.text = LobbyHandler.ServerSettings.ServerName;
                    MaxPlayers.text = LobbyHandler.ServerSettings.MaxPlayers.ToString();
                    Locked.isOn = LobbyHandler.ServerSettings.Locked;
                    Difficulty.value = LobbyHandler.ServerSettings.Difficulty;
                    WorldSeed.text = LobbyHandler.ServerSettings.WorldSeed;
                    WorldSize.value = (int)LobbyHandler.ServerSettings.WorldSize;
                    WorldType.value = (int)LobbyHandler.ServerSettings.WorldType;
                    WorldRivers.value = (int)LobbyHandler.ServerSettings.WorldRivers;

                    World.inst.mapBias = LobbyHandler.ServerSettings.WorldType;
                    World.inst.mapRiverLakes = LobbyHandler.ServerSettings.WorldRivers;
                    World.inst.mapSize = LobbyHandler.ServerSettings.WorldSize;


                    Player.inst.difficulty = (Player.Difficulty)Difficulty.value;
                }
                else if (KCServer.IsRunning)
                {
                    LobbyHandler.ServerSettings.ServerName = ServerName.text;
                    LobbyHandler.ServerSettings.MaxPlayers = int.Parse(MaxPlayers.text.Length == 0 ? "0" : MaxPlayers.text);
                    LobbyHandler.ServerSettings.Password = Password.text;
                    LobbyHandler.ServerSettings.Locked = Locked.isOn;
                    LobbyHandler.ServerSettings.Difficulty = Difficulty.value;
                    LobbyHandler.ServerSettings.WorldSeed = WorldSeed.text;
                    LobbyHandler.ServerSettings.WorldSize = (World.MapSize)WorldSize.value;
                    LobbyHandler.ServerSettings.WorldType = (World.MapBias)WorldType.value;
                    LobbyHandler.ServerSettings.WorldRivers = (World.MapRiverLakes)WorldRivers.value;

                    World.inst.mapBias = LobbyHandler.ServerSettings.WorldType;
                    World.inst.mapRiverLakes = LobbyHandler.ServerSettings.WorldRivers;
                    World.inst.mapSize = LobbyHandler.ServerSettings.WorldSize;

                    Player.inst.difficulty = (Player.Difficulty)Difficulty.value;

                    StartGameButton.interactable = Main.kCPlayers.Values.Skip(1).All(player => player.ready);
                    NewWorldButton.interactable = !LobbyManager.loadingSave;

                    if (Main.kCPlayers.Count > 0)
                        LobbyHandler.ServerSettings.SendToAll(KCClient.client.Id);
                }
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

        public void SetDetails(ServerEntryScript details)
        {
            try
            {
                Main.helper.Log("set details???");
                serverDetails = details;

                ServerName.text = serverDetails.Name;
                MaxPlayers.text = serverDetails.MaxPlayers.ToString();
                Locked.isOn = serverDetails.Locked;
                Difficulty.value = (int)Enum.Parse(typeof(Difficulties), serverDetails.Difficulty);

                Main.helper.Log("set details");

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
}
