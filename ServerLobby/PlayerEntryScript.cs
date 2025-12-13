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

            SetValues();

            InvokeRepeating("SetValues", 0, 0.25f);

            transform.Find("PlayerBanner").GetComponent<Button>().onClick.AddListener(() =>
            {
                Main.TransitionTo(MenuState.NameAndBanner);
            });
        }

        public void SetValues()
        {
            try
            {
                if (banner == null)
                {
                    var bannerTransform = transform.Find("PlayerBanner");
                    if (bannerTransform == null)
                        return;
                    banner = bannerTransform.GetComponent<RawImage>();
                    if (banner == null)
                        return;
                }

                if (!Main.clientSteamIds.TryGetValue(Client, out var steamId))
                    return;

                if (!Main.kCPlayers.TryGetValue(steamId, out var player) || player == null)
                    return;

                var nameTransform = transform.Find("PlayerName");
                if (nameTransform != null)
                    nameTransform.GetComponent<TextMeshProUGUI>().text = player.name ?? "";

                var readyTransform = transform.Find("Ready");
                if (readyTransform != null)
                    readyTransform.gameObject.SetActive(player.ready);

                if (World.inst == null || World.inst.liverySets == null)
                    return;

                if (player.banner < 0 || player.banner >= World.inst.liverySets.Length)
                    return;

                banner.texture = World.inst.liverySets[player.banner].banners;
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
