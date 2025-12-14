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

            // Hide server lobby
            Main.TransitionTo((MenuState)200);

            // This is run when user clicks "accept" on choose your map screeen

            try
            {
                int desiredSpeed = 1;
                if (!LobbyManager.loadingSave)
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

                    SpeedControlUI.inst.SetSpeed(desiredSpeed);
                }
                else
                {
                    LobbyManager.loadingSave = false;
                    GameState.inst.SetNewMode(GameState.inst.playingMode);
                    SpeedControlUI.inst.SetSpeed(desiredSpeed);
                }
            }
            catch (Exception ex)
            {
                // Handle exception here
                Main.helper.Log(ex.Message.ToString());
                Main.helper.Log(ex.ToString());
            }
        }

        public override void HandlePacketClient()
        {
            Start();
        }

        public override void HandlePacketServer()
        {
            //Start();


            /*AIBrainsContainer.PreStartAIConfig aiConfig = new AIBrainsContainer.PreStartAIConfig();
            int count = 0;
            for (int i = 0; i < RivalKingdomSettingsUI.inst.rivalItems.Length; i++)
            {
                RivalItemUI r = RivalKingdomSettingsUI.inst.rivalItems[i];
                bool flag = r.Enabled && !r.Locked;
                if (flag)
                {
                    count++;
                }
            }
            int idx = 0;
            aiConfig.startData = new AIBrainsContainer.PreStartAIConfig.AIStartData[count];
            for (int j = 0; j < RivalKingdomSettingsUI.inst.rivalItems.Length; j++)
            {
                RivalItemUI item = RivalKingdomSettingsUI.inst.rivalItems[j];
                bool flag2 = item.Enabled && !item.Locked;
                if (flag2)
                {
                    aiConfig.startData[idx] = new AIBrainsContainer.PreStartAIConfig.AIStartData();
                    aiConfig.startData[idx].landmass = item.flag.landmass;
                    aiConfig.startData[idx].bioCode = item.bannerIdx;
                    aiConfig.startData[idx].personalityKey = PersonalityCollection.aiPersonalityKeys[0];
                    aiConfig.startData[idx].skillLevel = item.GetSkillLevel();
                    idx++;
                }
            }
            AIBrainsContainer.inst.aiStartInfo = aiConfig;
            bool isControllerActive = GamepadControl.inst.isControllerActive;
            if (isControllerActive)
            {
                ConsoleCursorMenu.inst.PrepForGamepad();
            }*/
        }
    }
}
