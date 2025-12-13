using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KCM.ServerLobby.LobbyChat
{
    public class ChatEntryScript : MonoBehaviour
    {
        public ushort Client { get; set; }
        public string PlayerName { get; set; }
        public string Message { get; set; }
        RawImage banner { get; set; }

        public void Start()
        {
            try
            {
                transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = PlayerName;
                transform.Find("PlayerMessage").GetComponent<TextMeshProUGUI>().text = Message;

                banner = transform.Find("PlayerBanner").GetComponent<RawImage>();

                InvokeRepeating("SetValues", 0, 0.25f);
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

        public void SetValues()
        {
            try
            {
                KCPlayer player;
                Main.kCPlayers.TryGetValue(Main.GetPlayerByClientID(Client).steamId, out player);

                var bannerTexture = World.inst.liverySets[player.banner].banners;
                
                banner.texture = bannerTexture;
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
