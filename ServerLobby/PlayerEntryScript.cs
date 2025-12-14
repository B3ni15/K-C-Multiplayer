using KCM.Enums;
using KCM.Packets;
using KCM.Packets.Handlers;
using Riptide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KCM.ServerLobby
{
    public class PlayerEntryScript : MonoBehaviour
    {
        public ushort Client { get; set; }

        RawImage banner { get; set; }

        public void Start()
        {
            banner = transform.Find("PlayerBanner").GetComponent<RawImage>();

            transform.Find("PlayerBanner").GetComponent<Button>().onClick.AddListener(() =>
            {
                Main.TransitionTo(MenuState.NameAndBanner);//ChooseBannerUI Hooks required, as well as townnameui
            });

            SetValues();
            InvokeRepeating("SetValues", 0, 0.25f);
        }

        public void SetValues()
        {
            try
            {
                KCPlayer player;
                Main.kCPlayers.TryGetValue(Main.GetPlayerByClientID(Client).steamId, out player);
                transform.Find("PlayerName").GetComponent<TextMeshProUGUI>().text = player.name;
                transform.Find("Ready").gameObject.SetActive(player.ready);

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
