using Assets.Code;
using Assets;
using KCM.Attributes;
using KCM.Packets.Lobby;
using KCM.Packets.Network;
using KCM.ServerLobby;
using KCM.ServerLobby.LobbyChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace KCM.Packets.Handlers
{
    public class LobbyHandler
    {
        public static ServerSettings ServerSettings = new ServerSettings();

        public static List<GameObject> playerEntries = new List<GameObject>();


        public static void ClearPlayerList()
        {
            try
            {
                foreach (GameObject entry in playerEntries)
                    GameObject.Destroy(entry);

                playerEntries.Clear();

                if (!KCServer.IsRunning)
                {
                    Main.kCPlayers.Clear();
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

        public static void AddPlayerEntry(ushort client)
        {
            try
            {
                GameObject entry = GameObject.Instantiate(PrefabManager.serverLobbyPlayerEntryPrefab, ServerLobbyScript.PlayerListContent);
                entry.SetActive(true);
                Main.helper.Log(entry.ToString());
                var s = entry.AddComponent<PlayerEntryScript>();

                s.Client = client;

                playerEntries.Add(entry);
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

        public static void AddSystemMessage(string message)
        {
            try
            {
                GameObject entry = GameObject.Instantiate(PrefabManager.serverChatSystemEntryPrefab, ServerLobbyScript.PlayerChatContent);
                entry.SetActive(true);
                chatEntries.Add(entry);
                var s = entry.AddComponent<SystemEntryScript>();


                SnapTo(entry.GetComponent<RectTransform>());

                s.Message = message;
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

        public static List<GameObject> chatEntries = new List<GameObject>();

        public static void AddChatMessage(ushort client, string player, string message)
        {
            try
            {
                GameObject entry = GameObject.Instantiate(PrefabManager.serverChatEntryPrefab, ServerLobbyScript.PlayerChatContent);
                entry.SetActive(true);

                chatEntries.Add(entry);

                var s = entry.AddComponent<ChatEntryScript>();

                SnapTo(entry.GetComponent<RectTransform>());

                s.Client = client;
                s.PlayerName = player;
                s.Message = message;
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

        public static void ClearChatEntries()
        {
            try
            {
                foreach (GameObject entry in chatEntries)
                    GameObject.Destroy(entry);

                chatEntries.Clear();
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


        public static void SnapTo(RectTransform target)
        {
            Canvas.ForceUpdateCanvases();

            target.parent.parent.parent.GetComponent<ScrollRect>().normalizedPosition = new Vector2(0, 0);
        }
    }
}
