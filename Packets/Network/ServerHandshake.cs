using KCM.Enums;
using KCM.Packets.Lobby;
using Riptide;
using Riptide.Demos.Steam.PlayerHosted;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KCM.Packets.Network
{
    public class ServerHandshake : Packet
    {
        public override ushort packetId => (int)Enums.Packets.ServerHandshake;

        public bool loadingSave { get; set; }

        public override void HandlePacketClient()
        {
            ModalManager.HideModal();

            Main.TransitionTo(Enums.MenuState.ServerLobby);

            SfxSystem.PlayUiSelect();

            Cam.inst.desiredDist = 80f;
            Cam.inst.desiredPhi = 45f;
            CloudSystem.inst.threshold1 = 0.6f;
            CloudSystem.inst.threshold2 = 0.8f;
            CloudSystem.inst.BaseFreq = 4.5f;
            Weather.inst.SetSeason(Weather.Season.Summer);

            //inst = new KCClient(KCServer.IsRunning ? "Ryan" : "Orion");
            KCClient.inst = new KCClient(SteamFriends.GetPersonaName());

            Main.helper.Log("Sending client connected. Client ID is: " + clientId);

            Main.kCPlayers.Add(Main.PlayerSteamID, new KCPlayer(KCClient.inst.Name, clientId, Main.PlayerSteamID));

            Player.inst.PlayerLandmassOwner.teamId = clientId * 10 + 2;

            if (loadingSave && KCServer.IsRunning)
                Main.TransitionTo(MenuState.Load);
            else if (!loadingSave)
            {
                Main.TransitionTo(MenuState.NameAndBanner);

            }


            new KingdomName() { kingdomName = TownNameUI.inst.townName, clientId = clientId }.Send();

            new ClientConnected()
            {
                clientId = clientId,
                Name = KCClient.inst.Name,
                SteamId = Main.PlayerSteamID
            }.Send();
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }
    }
}
