using Harmony;
using KCM.Enums;
using KCM.Packets;
using KCM.Packets.Handlers;
using KCM.Packets.Lobby;
using KCM.Packets.Network;
using Riptide;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static KCM.KCServer;

namespace KCM
{
    public class KCClient : MonoBehaviour
    {
        public static Client client = new Client(Main.steamClient);

        public string Name { get; set; }

        public static KCClient inst { get; set; }


        static KCClient()
        {
            client.Connected += Client_Connected;
            client.ConnectionFailed += Client_ConnectionFailed;
            client.Disconnected += Client_Disconnected;
            client.MessageReceived += PacketHandler.HandlePacket;
        }

        private static void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            Main.helper.Log("Client disconnected event start");
            try
            {
                Main.ResetMultiplayerState("Client disconnected");

                if (e.Message != null)
                {
                    Main.helper.Log(e.Message.ToString());
                    MessageReceivedEventArgs eargs = new MessageReceivedEventArgs(null, (ushort)Enums.Packets.ShowModal, e.Message);

                    if (eargs.MessageId == (ushort)Enums.Packets.ShowModal)
                    {
                        ShowModal modalPacket = (ShowModal)PacketHandler.DeserialisePacket(eargs);

                        modalPacket.HandlePacketClient();
                    }
                }
                else
                {

                    GameState.inst.SetNewMode(GameState.inst.mainMenuMode);
                    ModalManager.ShowModal("Disconnected from Server", ErrorCodeMessages.GetMessage(e.Reason), "Okay", true, () => { Main.TransitionTo(MenuState.ServerBrowser); });
                }

            }
            catch (Exception ex)
            {
                Main.helper.Log("Error handling disconnection message");
                Main.helper.Log(ex.ToString());
            }
            Main.helper.Log("Client disconnected event end");
        }

        private static void Client_ConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            Main.helper.Log($"Connection failed: {e.Reason}");

            ModalManager.ShowModal("Failed to connect", ErrorCodeMessages.GetMessage(e.Reason));
        }

        private static void Client_Connected(object sender, EventArgs e)
        {
            try
            {
                if (client != null && client.Connection != null)
                {
                    client.Connection.CanQualityDisconnect = false;
                    client.Connection.MaxSendAttempts = 50;
                }
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error configuring client connection");
                Main.helper.Log(ex.ToString());
            }

        }

        public KCClient(string name)
        {
            Name = name;
        }

        public static void Connect(string ip)
        {
            Main.helper.Log("Trying to connect to: " + ip);
            client.Connect(ip, useMessageHandlers: false);
        }

        private void Update()
        {
            client.Update();
        }

        private void Preload(KCModHelper helper)
        {

            helper.Log("Preload run in client");
        }

        private void SceneLoaded(KCModHelper helper)
        {
        }
    }
}
