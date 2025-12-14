using KCM;
using KCM.Enums;
using KCM.Packets.Handlers;
using Steamworks;
using UnityEngine;

namespace Riptide.Demos.Steam.PlayerHosted
{
    public class LobbyManager : MonoBehaviour
    {
        private static LobbyManager _singleton;
        internal static LobbyManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(LobbyManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        protected Callback<LobbyCreated_t> lobbyCreated;
        protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
        protected Callback<LobbyEnter_t> lobbyEnter;

        private const string HostAddressKey = "HostAddress";
        private CSteamID lobbyId;

        private void Awake()
        {
            Singleton = this;
        }

        private void Start()
        {

            if (!KCMSteamManager.Initialized)
            {
                Main.helper.Log("Steam is not initialized!");
                return;
            }

            lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
            gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
            lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);

        }

        public static bool loadingSave = false;

        internal void CreateLobby(bool loadingSave = false)
        {
            var result = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 25);

            LobbyManager.loadingSave = loadingSave;

        }

        private void OnLobbyCreated(LobbyCreated_t callback)
        {

            if (callback.m_eResult != EResult.k_EResultOK)
            {
                //UIManager.Singleton.LobbyCreationFailed();
                Main.helper.Log("Create lobby failed");
                return;
            }

            lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            //UIManager.Singleton.LobbyCreationSucceeded(callback.m_ulSteamIDLobby);

            //NetworkManager.Singleton.Server.Start(0, 5, NetworkManager.PlayerHostedDemoMessageHandlerGroupId);


            KCServer.StartServer();

            Main.TransitionTo(MenuState.ServerLobby);


            try
            {
                Main.helper.Log("About to call connect");
                KCClient.Connect("127.0.0.1");

                World.inst.Generate();
                ServerLobbyScript.WorldSeed.text = World.inst.GetTextSeed();

                LobbyHandler.ClearPlayerList();

                /*Cam.inst.desiredDist = 80f;
                Cam.inst.desiredPhi = 45f;
                CloudSystem.inst.threshold1 = 0.6f;
                CloudSystem.inst.threshold2 = 0.8f;
                CloudSystem.inst.BaseFreq = 4.5f;
                Weather.inst.SetSeason(Weather.Season.Summer);


                Main.TransitionTo(MenuState.NameAndBanner);*/

                ServerBrowser.registerServer = true;
            }
            catch (System.Exception ex)
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

            //NetworkManager.Singleton.Client.Connect("127.0.0.1", messageHandlerGroupId: NetworkManager.PlayerHostedDemoMessageHandlerGroupId);
        }

        internal void JoinLobby(ulong lobbyId)
        {
            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        }

        private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
        {
            SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        }

        private void OnLobbyEnter(LobbyEnter_t callback)
        {
            if (KCServer.IsRunning)
                return;

            lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            CSteamID hostId = SteamMatchmaking.GetLobbyOwner(lobbyId);

            KCClient.Connect(hostId.ToString());
            //UIManager.Singleton.LobbyEntered();
        }

        public void LeaveLobby()
        {
            //NetworkManager.Singleton.StopServer();
            //NetworkManager.Singleton.DisconnectClient();
            SteamMatchmaking.LeaveLobby(lobbyId);

            if (KCClient.client.IsConnected)
                KCClient.client.Disconnect();

            Main.helper.Log("clear players");
            Main.kCPlayers.Clear();
            LobbyHandler.ClearPlayerList();
            LobbyHandler.ClearChatEntries();
            Main.helper.Log("end clear players");

            if (KCServer.IsRunning)
                KCServer.server.Stop();



            Main.TransitionTo(MenuState.ServerBrowser);
            ServerBrowser.registerServer = false;
        }
    }
}
