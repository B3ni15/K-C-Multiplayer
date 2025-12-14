using KCM.Enums;
using Riptide.Demos.Steam.PlayerHosted;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Lobby
{
    public class StartGame : Packet
    {
        public override ushort packetId => (int)Enums.Packets.StartGame;

        public void Start()
        {
            Main.helper.Log(GameState.inst.mainMenuMode.ToString());

            Main.TransitionTo((MenuState)200);

            try
            {
                SpeedControlUI.inst.SetSpeed(0);

                try
                {
                    typeof(MainMenuMode).GetMethod("StartGame", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(GameState.inst.mainMenuMode, null);
                }
                catch (Exception ex)
                {
                    Main.helper.Log(ex.Message.ToString());
                    Main.helper.Log(ex.ToString());
                }

                SpeedControlUI.inst.SetSpeed(0);
            }
            catch (Exception ex)
            {
                Main.helper.Log(ex.Message.ToString());
                Main.helper.Log(ex.ToString());
            }
        }

        public override void HandlePacketClient()
        {
            if (!LobbyManager.loadingSave)
            {
                Start();
            }
            else
            {
                ServerLobbyScript.LoadingSave.SetActive(true);
            }
        }

        public override void HandlePacketServer()
        {
        }
    }
}
