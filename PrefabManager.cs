using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM
{
    public class PrefabManager
    {
        public static AssetBundle assetBundle;
        public static GameObject serverBrowserPrefab;
        public static GameObject serverEntryItemPrefab;

        public static GameObject serverLobbyPrefab;
        public static GameObject serverLobbyPlayerEntryPrefab;
        public static GameObject serverChatEntryPrefab;
        public static GameObject serverChatSystemEntryPrefab;

        public static GameObject modalUIPrefab;

        public void PreScriptLoad(KCModHelper _helper)
        {
            try
            {
                //Main.helper = _helper;

                assetBundle = KCModHelper.LoadAssetBundle(_helper.modPath, "serverbrowserpkg");

                Main.helper.Log(String.Join(", ", assetBundle.GetAllAssetNames()));

                serverBrowserPrefab = assetBundle.LoadAsset("assets/workspace/serverbrowser.prefab") as GameObject;
                serverEntryItemPrefab = assetBundle.LoadAsset("assets/workspace/serverentryitem.prefab") as GameObject;


                serverLobbyPrefab = assetBundle.LoadAsset("assets/workspace/serverlobby.prefab") as GameObject;
                serverLobbyPlayerEntryPrefab = assetBundle.LoadAsset("assets/workspace/serverlobbyplayerentry.prefab") as GameObject;
                serverChatEntryPrefab = assetBundle.LoadAsset("assets/workspace/serverchatentry.prefab") as GameObject;
                serverChatSystemEntryPrefab = assetBundle.LoadAsset("assets/workspace/serverchatsystementry.prefab") as GameObject;

                modalUIPrefab = assetBundle.LoadAsset("assets/workspace/modalui.prefab") as GameObject;

                Main.helper.Log("Loaded assets");
            }
            catch (Exception ex)
            {
                Main.helper.Log(ex.ToString());
                Main.helper.Log(ex.Message);
                Main.helper.Log(ex.StackTrace);
            }
        }
    }
}
