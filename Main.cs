using Assets.Code;
using Assets.Code.UI;
using Assets.Interface;
using Harmony;
using KCM.Enums;
using KCM.LoadSaveOverrides;
using KCM.Packets.Game;
using KCM.Packets.Game.Dragon;
using KCM.Packets.Game.GameBuilding;
using KCM.Packets.Game.GamePlayer;
using KCM.Packets.Game.GameTrees;
using KCM.Packets.Game.GameVillager;
using KCM.Packets.Game.GameWeather;
using KCM.Packets.Game.GameWorld;
using KCM.Packets.Handlers;
using KCM.Packets.Lobby;
using KCM.StateManagement.BuildingState;
using KCM.StateManagement.Observers;
using KCM.UI;
using Newtonsoft.Json;
using Riptide;
using Riptide.Demos.Steam.PlayerHosted;
using Riptide.Transports.Steam;
using Riptide.Utils;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static ModCompiler;
using static World;

namespace KCM
{
    public class Main : MonoBehaviour
    {
        public static KCModHelper helper;
        public static MenuState menuState = (MenuState)MainMenuMode.State.Uninitialized;

        public static Dictionary<string, KCPlayer> kCPlayers = new Dictionary<string, KCPlayer>();
        public static Dictionary<ushort, string> clientSteamIds = new Dictionary<ushort, string>();

        public static KCPlayer GetPlayerByClientID(ushort clientId)
        {
            return kCPlayers[clientSteamIds[clientId]];
        }

        public static Player GetPlayerByTeamID(int teamId) // Need to replace building / production types so that the correct player is used. IResourceStorage and IResourceProvider, and jobs
        {
            try
            {
                var player = kCPlayers.Values.FirstOrDefault(p => p.inst.PlayerLandmassOwner.teamId == teamId).inst;

                return player;
            }
            catch (Exception e)
            {
                if (KCServer.IsRunning || KCClient.client.IsConnected)
                {
                    Main.helper.Log("Failed finding player by teamID: " + teamId + " My teamID is: " + Player.inst.PlayerLandmassOwner.teamId);
                    Main.helper.Log(kCPlayers.Count.ToString());
                    Main.helper.Log(string.Join(", ", kCPlayers.Values.Select(p => p.inst.PlayerLandmassOwner.teamId.ToString())));
                    Main.helper.Log(e.Message);
                    Main.helper.Log(e.StackTrace);
                }
            }
            return Player.inst;
        }

        public static Player GetPlayerByBuilding(Building building)
        {
            try
            {
                var lmo = World.GetLandmassOwner(building.LandMass());

                if (lmo == null) // Return the actual player for the client if the landmass owner is null
                    return Player.inst;

                // Return the player by teamId so that the correct player instance is updated/used on the server
                return GetPlayerByTeamID(building.TeamID());
            }
            catch (Exception e)
            {
                Main.helper.Log("Failed finding player by building: " + building.UniqueName);
                Main.helper.Log(e.Message);
                Main.helper.Log(e.StackTrace);
            }
            return Player.inst;
        }

        public static string PlayerSteamID = SteamUser.GetSteamID().ToString();

        public static KCMSteamManager KCMSteamManager = null;
        public static SteamServer steamServer = new SteamServer();
        public static Riptide.Transports.Steam.SteamClient steamClient = new Riptide.Transports.Steam.SteamClient(steamServer);

        public static ushort currentClient = 0;

        #region "SceneLoaded"
        private void SceneLoaded(KCModHelper helper)
        {
            helper.Log("SceneLoaded run in main");
            RiptideLogger.Initialize(helper.Log, helper.Log, helper.Log, helper.Log, false);

            helper.Log($"{SteamFriends.GetPersonaName()}");


            KCMSteamManager = new GameObject("KCMSteamManager").AddComponent<KCMSteamManager>();
            DontDestroyOnLoad(KCMSteamManager);

            var lobbyManager = new GameObject("LobbyManager").AddComponent<LobbyManager>();
            DontDestroyOnLoad(lobbyManager);

            //SteamFriends.InviteUserToGame(new CSteamID(76561198036307537), "test");
            //SteamMatchmaking.lobby

            //Main.helper.Log($"Timer duration for hazardpay {Player.inst.hazardPayWarmup.Duration}");

            try
            {

                SteamFriends.SetRichPresence("status", "Playing Multiplayer");

                PacketHandler.Initialise();

                Main.helper.Log(JsonConvert.SerializeObject(World.inst.mapSizeDefs, Formatting.Indented));

                KaC_Button serverBrowser = new KaC_Button(Constants.MainMenuUI_T.Find("TopLevelUICanvas/TopLevel/Body/ButtonContainer/New").parent)
                {
                    Name = "Multiplayer",
                    Text = "Multiplayer",
                    FirstSibling = true,
                    OnClick = () =>
                    {
                        //Constants.MainMenuUI_T.Find("TopLevelUICanvas/TopLevel").gameObject.SetActive(false);
                        SfxSystem.PlayUiSelect();

                        //ServerBrowser.serverBrowserRef.SetActive(true);
                        TransitionTo(MenuState.ServerBrowser);
                    }
                };
                serverBrowser.Transform.SetSiblingIndex(2);


                Destroy(Constants.MainMenuUI_T.Find("TopLevelUICanvas/TopLevel/Body/ButtonContainer/Kingdom Share").gameObject);
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
        #endregion

        public static int FixedUpdateInterval = 0;

        public static void ClearVillagerPositionCache()
        {
            // Kept for API compatibility with LobbyManager
        }

        private void FixedUpdate()
        {
            FixedUpdateInterval++;
        }

        #region "TransitionTo"
        public static void TransitionTo(MenuState state)
        {
            try
            {
                ServerBrowser.serverBrowserRef.SetActive(state == MenuState.ServerBrowser);
                ServerBrowser.serverLobbyRef.SetActive(state == MenuState.ServerLobby);

                ServerBrowser.KCMUICanvas.gameObject.SetActive((int)state > 21);
                helper.Log(((int)state > 21).ToString());

                GameState.inst.mainMenuMode.TransitionTo((MainMenuMode.State)state);
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
        #endregion

        private void Preload(KCModHelper helper)
        {
            helper.Log("Preload start in main");
            try
            {


                //MainMenuPatches.Patch();
                Main.helper = helper;
                helper.Log(helper.modPath);

                var harmony = HarmonyInstance.Create("harmony");
                harmony.PatchAll(Assembly.GetExecutingAssembly());


                helper.Log("Preload run in main");
            }
            catch (Exception ex)
            {
                Main.helper.Log("----------------------- Main exception -----------------------");
                Main.helper.Log(ex.ToString());
                Main.helper.Log("----------------------- Main message -----------------------");
                Main.helper.Log(ex.Message);
                Main.helper.Log("----------------------- Main stacktrace -----------------------");
                Main.helper.Log(ex.StackTrace);
            }
            helper.Log("Preload end in main");
        }

        #region "MainMenu Hooks"

        public static MenuState prevMenuState = MenuState.Uninitialized;

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("TransitionTo")]
        public class TransitionToHook
        {
            private static void Prefix(MainMenuMode.State newState)
            {
                Main.helper.Log($"Menu set to: {(MenuState)newState}");

                Main.prevMenuState = Main.menuState;

                if (newState != MainMenuMode.State.Uninitialized)
                    Main.menuState = (MenuState)newState;
            }
        }

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("OnClickedClose")]
        public class OnClickedCloseHook
        {
            private static bool Prefix()
            {
                helper.Log("Transition back");

                TransitionTo(prevMenuState);

                return false;
            }
        }

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("OnClickedBackToModeSelect")]
        public class OnClickedBackToModeSelectPatch
        {
            private static bool Prefix()
            {
                if (KCClient.client.IsConnected)
                {
                    Main.TransitionTo(MenuState.ServerLobby);
                    SfxSystem.PlayUiCancel();

                    return false;
                }
                else return true;
            }
        }

        [HarmonyPatch(typeof(MainMenuMode))]
        [HarmonyPatch("OnClickedAcceptNameBanner")]
        public class OnClickedAcceptNameBannerPatch
        {
            private static bool Prefix()
            {
                if (KCClient.client.IsConnected)
                {
                    Main.TransitionTo(MenuState.ServerLobby);
                    SfxSystem.PlayUiCancel();

                    return false;
                }
                else return true;
            }
        }
        #endregion

        #region "TownName Hooks"
        [HarmonyPatch(typeof(TownNameUI))]
        [HarmonyPatch("SetTownNameQuiet")]
        public static class TownNameHook
        {
            //A function to run after target function invocation
            private static void Postfix(TownNameUI __instance)
            {
                helper.Log($"name set: {__instance.townName}");

                new KingdomName() { kingdomName = __instance.townName }.Send();
            }
        }
        #endregion

        #region "ChooseBanner Hooks"
        [HarmonyPatch(typeof(ChooseBannerUI))]
        [HarmonyPatch("OnAccept")]
        public class ChooseBannerUIOnAcceptHook
        {
            private static void Postfix()
            {
                if (KCClient.client.IsConnected)
                {
                    var banner = Player.inst.PlayerLandmassOwner.bannerIdx;


                    new PlayerBanner() { banner = banner }.Send();
                    //return true;
                }
                //else return true;
            }
        }
        #endregion


        [HarmonyPatch(typeof(Keep))]
        [HarmonyPatch("OnPlayerPlacement")]
        public class KeepHook
        {
            public static void Postfix()
            {
                // Your code here

                // Get the name of the last method that called OnPlayerPlacement
                string callTree = "";
                List<string> strings = new List<string>();

                for (int i = 1; i < 10; i++)
                {
                    try
                    {
                        string callingMethodName = new StackFrame(i).GetMethod().Name;
                        strings.Add(callingMethodName);
                    }
                    catch
                    {
                        strings.Add("Start");
                        break;
                    }
                }

                strings.Reverse();

                Main.helper.Log($"Last {strings.Count} methods in call tree: {string.Join(" -> ", strings)}");
            }
        }

        #region "GameUI Hooks"
        //GameUI hook for acceptcursorobjplacement
        /*[HarmonyPatch(typeof(GameUI), "AcceptCursorObjPlacement")]
        public class AcceptCursorObjPlacementHook
        {
        }*/
        #endregion

        #region "World Hooks"
        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("Place")]
        public class PlaceHook
        {
            /*public static bool Prefix()
            {
                if (KCClient.client.IsConnected && !KCServer.IsRunning)
                {
                    if (!new StackFrame(3).GetMethod().kingdomName.Contains("HandlePacket"))
                        return false;
                }

                return true;
            }*/

            public static void Postfix(Building PendingObj)
            {
                try
                {
                    if (KCClient.client.IsConnected)
                    {
                        /*string callTree = "";
                        List<string> strings = new List<string>();

                        for (int i = 1; i < 10; i++)
                        {
                            try
                            {
                                string callingMethodName = new StackFrame(i).GetMethod().Name;
                                strings.Add($"{callingMethodName} ({i})");
                            }
                            catch
                            {
                                strings.Add("Start");
                                break;
                            }
                        }

                        strings.Reverse();

                        Main.helper.Log($"WORLDPLACE Last {strings.Count} methods in call tree: {string.Join(" -> ", strings)}");*/

                        if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket") && !new StackFrame(2).GetMethod().Name.Equals("RandomPlacement"))
                            return;

                        Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().Name}");
                        Main.helper.Log($"{KCClient.client.Id} {Main.kCPlayers[PlayerSteamID].name} - Sending building place packet for " + PendingObj.UniqueName);

                        // Need to batch building placements to prevent network spam
                        new WorldPlace()
                        {
                            uniqueName = PendingObj.UniqueName,
                            customName = PendingObj.customName,
                            guid = PendingObj.guid,
                            rotation = PendingObj.transform.GetChild(0).rotation,
                            globalPosition = PendingObj.transform.position,
                            localPosition = PendingObj.transform.GetChild(0).localPosition,
                            built = PendingObj.IsBuilt(),
                            placed = PendingObj.IsPlaced(),
                            open = PendingObj.Open,
                            doBuildAnimation = PendingObj.doBuildAnimation,
                            constructionPaused = PendingObj.constructionPaused,
                            constructionProgress = PendingObj.constructionProgress,
                            life = PendingObj.Life,
                            ModifiedMaxLife = PendingObj.ModifiedMaxLife,
                            //CollectForBuild = CollectForBuild,
                            yearBuilt = PendingObj.YearBuilt,
                            decayProtection = PendingObj.decayProtection,
                            seenByPlayer = PendingObj.seenByPlayer
                        }.Send();
                        //return true;
                    }
                    //else return true;
                }
                catch (Exception e)
                {
                    Main.helper.Log("World Place error");
                    Main.helper.Log(e.Message);
                    Main.helper.Log(e.StackTrace);
                }
            }
        }

        [HarmonyPatch(typeof(World), "RelationBetween")]
        public class WorldRelationBetweenHook
        {
            public static void Prefix(ref int teamIDA, ref int teamIDB)
            {

                //Main.helper.Log($"RelationBetween {teamIDA} and {teamIDB}");

                if (KCClient.client.IsConnected)
                {
                    if (teamIDA == 0 || teamIDB == 0)
                    {
                        if (teamIDA == 0)
                            teamIDA = Player.inst.PlayerLandmassOwner.teamId;

                        if (teamIDB == 0)
                            teamIDB = Player.inst.PlayerLandmassOwner.teamId;
                    }
                }
            }
        }
        #endregion


        #region "Player Hooks"

        [HarmonyPatch(typeof(Player), "Reset")]
        public class PlayerResetHook
        {
            public static bool Prefix(Player __instance)
            {
                if (KCClient.client.IsConnected && __instance.gameObject.name.Contains("Client Player") && !LobbyManager.loadingSave)
                {
                    try
                    {
                        var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

                        __instance.GetType().GetField("resetting", bindingFlags).SetValue(__instance, true);
                        //__instance.resetting = true;
                        __instance.GetType().GetField("poorHealthGracePeriod", bindingFlags).SetValue(__instance, 0f);
                        //__instance.poorHealthGracePeriod = 0f;
                        __instance.PlayerLandmassOwner.Gold = 0;
                        __instance.CurrYear = 0;

                        __instance.buildingDamageAnimator.Reset();
                        __instance.fruitSystem.Reset();
                        __instance.fieldSystem.Reset();

                        bool flag = __instance.DamagedList != null;
                        if (flag)
                        {
                            for (int i = 0; i < __instance.DamagedList.Length; i++)
                            {
                                __instance.DamagedList[i].Clear();
                            }
                            __instance.DamagedList = null;
                        }

                        __instance.irrigation.Reset();
                        __instance.ClearRegistry();
                        bool flag2 = __instance.buildingContainer;
                        if (flag2)
                        {
                            Building[] buildings = __instance.buildingContainer.transform.GetComponentsInChildren<Building>();
                            for (int j = 0; j < buildings.Length; j++)
                            {
                                buildings[j].destroyedWhileInPlay = false;
                                UnityEngine.Object.Destroy(buildings[j].gameObject);
                            }
                        }
                        UnityEngine.Object.Destroy(__instance.buildingContainer);
                        __instance.buildingContainer = new GameObject();
                        __instance.buildingContainer.name = "Buildings";

                        for (int k = 0; k < __instance.Workers.Count; k++)
                        {
                            __instance.Workers.data[k].Shutdown();
                        }
                        __instance.Workers.Clear();
                        /*int r = 0;
                        for (int l = 0; l < __instance.Homeless.Count; l++)
                        {
                            bool flag3 = !__instance.Homeless.data[l].shutdown;
                            if (flag3)
                            {
                                r++;
                            }
                        }*/

                        __instance.Homeless.Clear();

                        __instance.Residentials.Clear();
                        __instance.Buildings.Clear();
                        __instance.RadiusBonuses.Clear();
                        __instance.WagePayers.Clear();

                        __instance.timeAtFailHappiness = 0f;
                        __instance.MaxGoldStorage = 0;
                        __instance.KingdomHappiness = 100;
                        ReflectionHelper.ClearPrivateListField<Player.HappinessInfo>(__instance, "landMassHappiness");
                        //__instance.landMassHappiness.Clear();
                        ReflectionHelper.ClearPrivateListField<Player.HealthInfo>(__instance, "landMassHealth");
                        //__instance.landMassHealth.Clear();
                        ReflectionHelper.ClearPrivateListField<Player.IntegrityInfo>(__instance, "landMassIntegrity");
                        //__instance.landMassIntegrity.Clear();
                        //__instance.HealthTimer.ForceExpire(); // TO-DO implement timer
                        __instance.happinessMods.Clear();
                        /*for (int m = 0; m < __instance.plagueDeathInfo.Count; m++)
                        {
                            __instance.plagueDeathInfo[m].deathQueue.Clear();
                            __instance.plagueDeathInfo[m].deaths = 0;
                            __instance.plagueDeathInfo[m].deathTime = 0f;
                        }*/
                        ReflectionHelper.ClearPrivateListField<Villager>(__instance, "OldAgeDeathQueue");
                        //__instance.OldAgeDeathQueue.Clear();
                        __instance.GetType().GetField("deathsThisYear", bindingFlags).SetValue(__instance, 0);
                        //__instance.deathsThisYear = 0;
                        __instance.ResetPerLandMassData();
                        __instance.ResetTaxRates();
                        __instance.ResetCreativeModeOptions();
                        __instance.PlayerLandmassOwner.ReleaseOwnership();

                        ReflectionHelper.ClearPrivateListField<Player.DockOpening>(__instance, "dockOpenings");
                        //__instance.dockOpenings.Clear();
                        __instance.GetType().GetField("resetting", bindingFlags).SetValue(__instance, false);
                        //__instance.resetting = false;
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log("Error in reset player hook");
                        Main.helper.Log(e.Message);
                        Main.helper.Log(e.StackTrace);
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "AddBuilding")]
        public class PlayerAddBuildingHook
        {
            static int step = 1;
            static void LogStep(bool reset = false)
            {
                if (reset)
                    step = 1;

                Main.helper.Log(step.ToString());
                step++;
            }

            public static bool Prefix(Player __instance, Building b)
            {
                try
                {
                    if (KCClient.client.IsConnected)
                    {
                        LogStep(true);
                        __instance.Buildings.Add(b);
                        IResourceStorage[] storages = b.GetComponents<IResourceStorage>();
                        LogStep();
                        for (int i = 0; i < storages.Length; i++)
                        {
                            bool flag = !storages[i].IsPrivate();
                            if (flag)
                            {
                                FreeResourceManager.inst.AddResourceStorage(storages[i]);
                            }
                        }
                        LogStep();
                        int landMass = b.LandMass();
                        Home res = b.GetComponent<Home>();
                        bool flag2 = res != null;
                        LogStep();
                        if (flag2)
                        {
                            __instance.Residentials.Add(res);
                            __instance.ResidentialsPerLandmass[landMass].Add(res);
                        }
                        WagePayer wagePayer = b.GetComponent<WagePayer>();
                        LogStep();
                        bool flag3 = wagePayer != null;
                        if (flag3)
                        {
                            __instance.WagePayers.Add(wagePayer);
                        }
                        RadiusBonus radiusBonus = b.GetComponent<RadiusBonus>();
                        LogStep();
                        bool flag4 = radiusBonus != null;
                        if (flag4)
                        {
                            __instance.RadiusBonuses.Add(radiusBonus);
                        }
                        LogStep();
                        var globalBuildingRegistry = __instance.GetType().GetField("globalBuildingRegistry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as ArrayExt<Player.BuildingRegistry>;
                        LogStep();
                        var landMassBuildingRegistry = __instance.GetType().GetField("landMassBuildingRegistry", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as ArrayExt<Player.LandMassBuildingRegistry>;
                        LogStep();
                        var unbuiltBuildingsPerLandmass = __instance.GetType().GetField("unbuiltBuildingsPerLandmass", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance) as ArrayExt<ArrayExt<Building>>;
                        LogStep();

                        __instance.AddToRegistry(globalBuildingRegistry, b);
                        LogStep();
                        __instance.AddToRegistry(landMassBuildingRegistry.data[landMass].registry, b);
                        LogStep();
                        landMassBuildingRegistry.data[landMass].buildings.Add(b);
                        LogStep();
                        bool flag5 = !b.IsBuilt();
                        if (flag5)
                        {
                            unbuiltBuildingsPerLandmass.data[landMass].Add(b);
                        }
                        LogStep();


                        return false;
                    }
                }
                catch (Exception e)
                {
                    Main.helper.Log("Error in add building hook");
                    Main.helper.Log(e.Message);
                    Main.helper.Log(e.StackTrace);
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "SetupInitialWorkers")]
        public class PlayerSetupInitialWorkersHook
        {
            public static void Postfix(Keep keep)
            {
                if (KCClient.client.IsConnected)
                {


                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new SetupInitialWorkersPacket()
                    {
                        keepGuid = keep.gameObject.GetComponent<Building>().guid
                    }.Send();
                }
            }
        }

        [HarmonyPatch(typeof(VillagerSystem), "AddVillager")]
        public class PlayerAddVillagerHook
        {
            public static void Postfix(Villager __result, Vector3 pos)
            {
                if (KCClient.client.IsConnected)
                {
                    try
                    {
                        if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                            return;

                        if (Enumerable.Range(0, 4).Select(i => new StackFrame(i).GetMethod()?.Name).Any(name => name?.Contains("unpack") == true)) // If called by unpack in the tree, do not run, since clients already unpacked villager data
                            return;

                        new AddVillagerPacket()
                        {
                            guid = __result.guid,
                            position = pos,  // Include villager spawn position
                        }.Send();
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log("Error in add villager hook");

                        Main.helper.Log(e.Message);
                        Main.helper.Log(e.StackTrace);
                    }
                }
            }
        }

        #endregion


        #region "Tree Hooks"


        [HarmonyPatch(typeof(TreeSystem), "FellTree")]
        public class TreeSystemFellTreeHook
        {
            /*static IEnumerable<MethodBase> TargetMethods()
            {
                var tmeth = typeof(TreeSystem).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static);

                return tmeth.Cast<MethodBase>();
            }*/

            public static void Postfix(MethodBase __originalMethod, Cell cell, int idx)
            {
                if (KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new FellTree()
                    {
                        idx = idx,
                        x = cell.x,
                        z = cell.z
                    }.Send();
                }
            }
        }

        [HarmonyPatch(typeof(TreeSystem), "ShakeTree")]
        public class TreeSystemShakeTreeHook
        {
            public static void Postfix(MethodBase __originalMethod, int idx)
            {
                if (KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new ShakeTree()
                    {
                        idx = idx
                    }.Send();
                }
            }
        }

        [HarmonyPatch(typeof(TreeSystem), "GrowTree")]
        public class TreeSystemGrowTreeHook
        {
            /*public static bool Prefix()
            {
                if (KCClient.client.IsConnected && !KCServer.IsRunning)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (!new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return false;

                    
                }

                return true;
            }*/

            // Only server should send this information
            public static void Postfix(MethodBase __originalMethod, Cell cell)
            {
                if (KCServer.IsRunning)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new GrowTree()
                    {
                        X = cell.x,
                        Z = cell.z
                    }.SendToAll(KCClient.client.Id);
                }
            }
        }

        #endregion

        #region "Weather Hooks"
        [HarmonyPatch(typeof(Weather), "ChangeWeather")]
        public class WeatherChangeWeatherHook
        {
            public static void Postfix(MethodBase __originalMethod, Weather.WeatherType type)
            {
                if (KCServer.IsRunning && KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    if (type != Weather.inst.currentWeather)
                        new ChangeWeather()
                        {
                            weatherType = (int)type
                        }.Send();
                }
            }
        }
        #endregion

        #region "Building Hooks"

        [HarmonyPatch(typeof(Building), "CompleteBuild")]
        public class BuildingCompleteBuildHook
        {
            public static bool Prefix(MethodBase __originalMethod, Building __instance)
            {
                if (KCClient.client.IsConnected)
                {
                    Main.helper.Log("Overridden complete build");
                    Player player = Main.GetPlayerByTeamID(__instance.TeamID());

                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");
                    typeof(Building).GetField("built", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, true);

                    __instance.UpdateMaterialSelection();
                    __instance.SendMessage("OnBuilt", SendMessageOptions.DontRequireReceiver);


                    typeof(Building).GetField("yearBuilt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, player.CurrYear);

                    typeof(Building).GetMethod("AddAllResourceProviders", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);


                    player.BuildingNowBuilt(__instance);

                    typeof(Building).GetMethod("TryAddJobs", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                    __instance.BakePathing();

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Building), "UpdateConstruction")]
        public class BuildingUpdateHook
        {
            public static void Prefix(Building __instance)
            {
                try
                {
                    if (KCClient.client.IsConnected)
                    {
                        if (__instance.TeamID() == Player.inst.PlayerLandmassOwner.teamId)
                            StateObserver.RegisterObserver(__instance, new string[] {
                                "customName", "guid", "UniqueName", "built", "placed", "open", "doBuildAnimation", "constructionPaused", "constructionProgress", "resourceProgress",
                                "Life", "ModifiedMaxLife", "CollectForBuild", "yearBuilt", "decayProtection", "seenByPlayer",
                            }, BuildingStateManager.BuildingStateChanged, BuildingStateManager.SendBuildingUpdate);

                        //StateObserver.Update(__instance);
                    }
                }
                catch (Exception e)
                {
                    helper.Log(e.ToString());
                    helper.Log(e.Message);
                    helper.Log(e.StackTrace);
                }
            }
        }

        #endregion

        #region "Time Hooks"
        // TimeManager TrySetSpeed hook
        [HarmonyPatch(typeof(SpeedControlUI), "SetSpeed")]
        public class SpeedControlUISetSpeedHook
        {
            private static long lastTime = 0;

            public static bool Prefix()
            {
                if (KCClient.client.IsConnected)
                {
                    if ((DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastTime) < 250) // Set speed spam fix / hack
                        return false;

                    if (!new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return false;
                }

                return true;
            }

            public static void Postfix(int idx, bool skipNextSfx)
            {
                if (KCClient.client.IsConnected)
                {
                    /*Main.helper.Log($"set speed Called by 0: {new StackFrame(0).GetMethod()} {new StackFrame(0).GetMethod().Name.Contains("HandlePacket")}");
                    Main.helper.Log($"set speed Called by 1: {new StackFrame(1).GetMethod()} {new StackFrame(1).GetMethod().Name.Contains("HandlePacket")}");
                    Main.helper.Log($"set speed Called by 2: {new StackFrame(2).GetMethod()} {new StackFrame(2).GetMethod().Name.Contains("HandlePacket")}");
                    Main.helper.Log($"set speed Called by 3: {new StackFrame(3).GetMethod()} {new StackFrame(3).GetMethod().Name.Contains("HandlePacket")}");*/

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new SetSpeed()
                    {
                        speed = idx
                    }.Send();

                    lastTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                }
            }
        }
        #endregion

        #region "SteamManager Hook"
        [HarmonyPatch]
        public class SteamManagerAwakeHook
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var meth = typeof(SteamManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                return meth.Cast<MethodBase>();
            }

            public static bool Prefix(MethodBase __originalMethod)
            {
                return false;
            }
        }
        #endregion

        #region "Dragon Hooks"

        #region "Dragon Spawn Hooks"
        [HarmonyPatch(typeof(DragonSpawn), "SpawnSiegeDragon")]
        public class DragonSpawnSpawnSiegeDragonHook
        {
            public static bool Prefix()
            {
                if (KCClient.client.IsConnected && !KCServer.IsRunning && !new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                    return false;

                return true;
            }
            public static void Postfix(MethodBase __originalMethod, Vector3 start)
            {
                if (KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new SpawnSiegeDragonPacket() { start = start }.Send();
                }
            }
        }

        [HarmonyPatch(typeof(DragonSpawn), "SpawnMamaDragon", new Type[] { typeof(Vector3) })]
        public class DragonSpawnSpawnMamaDragonHook
        {
            public static bool Prefix()
            {
                if (KCClient.client.IsConnected && !KCServer.IsRunning && !new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                    return false;

                return true;
            }
            public static void Postfix(MethodBase __originalMethod, Vector3 start)
            {
                if (KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new SpawnMamaDragonPacket() { start = start }.Send();
                }
            }
        }

        [HarmonyPatch(typeof(DragonSpawn), "SpawnBabyDragon", new Type[] { typeof(Vector3) })]
        public class DragonSpawnSpawnBabyDragonHook
        {
            public static bool Prefix()
            {
                if (KCClient.client.IsConnected && !KCServer.IsRunning && !new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                    return false;

                return true;
            }
            public static void Postfix(MethodBase __originalMethod, Vector3 start)
            {
                if (KCClient.client.IsConnected)
                {
                    //Main.helper.Log($"Called by: {new StackFrame(3).GetMethod().kingdomName}");

                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new SpawnBabyDragonPacket() { start = start }.Send();
                }
            }
        }
        #endregion

        #endregion

        #region "Villager Hooks"
        [HarmonyPatch(typeof(Villager), "TeleportTo")]
        public class VillagerTeleportToHook
        {
            public static void Postfix(Villager __instance, Vector3 newPos)
            {
                if (KCClient.client.IsConnected)
                {
                    if (new StackFrame(3).GetMethod().Name.Contains("HandlePacket"))
                        return;

                    new VillagerTeleportTo()
                    {
                        guid = __instance.guid,
                        pos = newPos
                    }.Send();
                }
            }
        }
        #endregion

        #region "Job Hooks"

        /*[HarmonyPatch(typeof(Job), "OnEmployeeQuit")]
        public class JobOnEmployeeQuitHook
        {
            public static Player oldPlayer;

            public static void Prefix(Job __instance)
            {
                if (KCClient.client.IsConnected)
                {
                    oldPlayer = Player.inst;

                    Player.inst = Main.GetPlayerByTeamID(World.GetLandmassOwner(__instance.employer.LandMass()).teamId);
                }
            }

            public static void Postfix(Job __instance)
            {
                if (KCClient.client.IsConnected)
                {
                    Player.inst = oldPlayer;
                }
            }
        }*/

        #endregion

        #region "LoadSave Hooks"
        [HarmonyPatch(typeof(LoadSave), "GetSaveDir")]
        public class LoadSaveGetSaveDirHook
        {
            public static bool Prefix(ref string __result)
            {
                Main.helper.Log("Get save dir");
                if (KCClient.client.IsConnected)
                {
                    if (KCServer.IsRunning)
                    {

                    }
                    __result = Application.persistentDataPath + "/Saves/Multiplayer";

                    return false;
                }

                __result = Application.persistentDataPath + "/Saves"; ;
                return true;
            }
        }

        [HarmonyPatch(typeof(LoadSave), "LoadAtPath")]
        public class LoadSaveLoadAtPathHook
        {
            //public static string saveFile = "";
            public static byte[] saveData = new byte[0];

            public static bool Prefix(string path, string filename, bool visitedWorld)
            {
                if (KCServer.IsRunning)
                {
                    Main.helper.Log("Trying to load multiplayer save");
                    LoadSave.LastLoadDirectory = path;
                    path = path + "/" + filename;


                    bool flag = !File.Exists(path);
                    if (!flag)
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Binder = new MultiplayerSaveDeserializationBinder();
                        saveData = File.ReadAllBytes(path);
                        Stream file = new FileStream(path, FileMode.Open);
                        try
                        {
                            MultiplayerSaveContainer loadData = (MultiplayerSaveContainer)bf.Deserialize(file);
                            loadData.Unpack(null);
                            Broadcast.OnLoadedEvent.Broadcast(new OnLoadedEvent());
                        }
                        catch (Exception e)
                        {
                            GameState.inst.mainMenuMode.TransitionTo(MainMenuMode.State.LoadError);
                            Main.helper.Log("Error loading save");
                            Main.helper.Log(e.Message);
                            Main.helper.Log(e.StackTrace);
                            throw;
                        }
                        finally
                        {
                            bool flag2 = file != null;
                            if (flag2)
                            {
                                file.Close();
                                file.Dispose();
                            }
                        }
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(LoadSave), "Load")]
        public class LoadSaveLoadHook
        {
            public static bool memoryStreamHook = false;

            public static byte[] saveBytes = new byte[0];

            public static MultiplayerSaveContainer saveContainer;

            public static bool Prefix()
            {
                if (memoryStreamHook)
                {
                    Main.helper.Log("Attempting to load save from server");

                    using (MemoryStream ms = new MemoryStream(saveBytes))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Binder = new MultiplayerSaveDeserializationBinder();
                        saveContainer = (MultiplayerSaveContainer)bf.Deserialize(ms);
                    }

                    memoryStreamHook = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(LoadSave), "Save")]
        public class LoadSaveSaveHook
        {
            private class OutData
            {
                // Token: 0x04002176 RID: 8566
                public string Path;

                // Token: 0x04002177 RID: 8567
                public MultiplayerSaveContainer LoadSaveContainer;
            }

            private static void OutToFile(object data)
            {
                OutData outData = (OutData)data;
                BinaryFormatter bf = new BinaryFormatter();
                Stream file = null;
                try
                {
                    file = new FileStream(outData.Path, FileMode.Create, FileAccess.Write);
                    bf.Serialize(file, outData.LoadSaveContainer);
                }
                catch (Exception e)
                {
                    LoadSave.AppendToLocalErrorLog(string.Concat(new string[]
                    {
                "Problem during save",
                Environment.NewLine,
                e.Message,
                Environment.NewLine,
                e.StackTrace
                    }));
                }
                finally
                {
                    bool flag = file != null;
                    if (flag)
                    {
                        file.Close();
                        file.Dispose();
                    }
                }
            }

            public static bool Prefix(string pathOverride, UnityAction onCompleteCallback, ref Thread __result)
            {
                if (KCServer.IsRunning)
                {
                    Directory.CreateDirectory(LoadSave.GetSaveDir());
                    Guid guid = Guid.NewGuid();
                    string path = (pathOverride != "") ? pathOverride : (LoadSave.GetSaveDir() + "/" + guid);
                    Directory.CreateDirectory(path);
                    Thread thread;
                    try
                    {
                        thread = new Thread(new ParameterizedThreadStart(OutToFile));

                        MultiplayerSaveContainer packedData = new MultiplayerSaveContainer().Pack(null);
                        Broadcast.OnSaveEvent.Broadcast(new OnSaveEvent());
                        thread.Start(new OutData
                        {
                            LoadSaveContainer = packedData,
                            Path = path + "/world"
                        });
                    }
                    catch (Exception e)
                    {
                        //LoadSave.ErrorToKingdomLog(e);
                        Main.helper.Log(e.Message);
                        Main.helper.Log(e.StackTrace);
                        throw;
                    }
                    finally
                    {
                        LoadSave.SaveWorldSummaryData(path);
                    }


                    // Custom banners not implemented yet
                    /*try
                    {
                        bool usingCustomBanner = Player.inst.usingCustomBanner;
                        if (usingCustomBanner)
                        {
                            File.WriteAllBytes(path + "/custombanner.png", Player.inst.customBannerTexture2D.EncodeToPNG());
                        }
                    }
                    catch (Exception e2)
                    {
                        Main.helper.Log(e2.Message);
                    }*/

                    try
                    {
                        World.inst.TakeScreenshot(path + "/cover", new Func<int, int, Texture2D>(World.inst.Func_CaptureWorldShot), onCompleteCallback);
                    }
                    catch (Exception e3)
                    {
                        Main.helper.Log(e3.Message);
                    }
                    bool flag = onCompleteCallback != null;
                    if (flag)
                    {
                        bool flag2 = thread != null && thread.ThreadState == System.Threading.ThreadState.Running;
                        if (flag2)
                        {
                            thread.Join();
                        }
                    }
                    GC.Collect();

                    __result = thread;

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(SaveLoadUI), "ClickLoadItem")]
        public class SaveLoadUIClickedLoadItemHook
        {
            public static bool Prefix(SaveLoadUI __instance, string id)
            {
                if (KCServer.IsRunning)
                {

                    LoadSave.Load(id);
                    TransitionTo(MenuState.ServerLobby);
                    //GameState.inst.SetNewMode(GameState.inst.playingMode);


                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Player.PlayerSaveData), "ProcessBuilding")]
        public class PlayerProcessBuildingHook
        {
            public static bool Prefix(Building.BuildingSaveData structureData, Player p, ref Building __result)
            {
                if (KCClient.client.IsConnected)
                {

                    Building Building = GameState.inst.GetPlaceableByUniqueName(structureData.uniqueName);
                    bool flag = Building;
                    if (flag)
                    {
                        Building building = UnityEngine.Object.Instantiate<Building>(Building);
                        building.transform.position = structureData.globalPosition;
                        building.Init();
                        building.transform.SetParent(p.buildingContainer.transform, true);
                        structureData.Unpack(building);
                        p.AddBuilding(building);

                        Main.helper.Log($"Loading player id: {p.PlayerLandmassOwner.teamId}");
                        Main.helper.Log($"loading building: {building.FriendlyName}");
                        Main.helper.Log($" (teamid: {building.TeamID()})");
                        Main.helper.Log(p.ToString());
                        bool flag2 = building.GetComponent<Keep>() != null && building.TeamID() == p.PlayerLandmassOwner.teamId;
                        Main.helper.Log("Set keep? " + flag2);
                        if (flag2)
                        {
                            p.keep = building.GetComponent<Keep>();
                            Main.helper.Log(p.keep.ToString());
                        }
                        __result = building;
                    }
                    else
                    {
                        Main.helper.Log(structureData.uniqueName + " failed to load correctly");
                        __result = null;
                    }

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Player.PlayerSaveData), "Pack")]
        public class PlayerSaveDataPackgHook
        {
            public static bool Prefix(Player.PlayerSaveData __instance, Player p, ref Player.PlayerSaveData __result)
            {
                if (KCClient.client.IsConnected)
                {
                    var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

                    Main.helper.Log("Running patched player pack method");
                    Main.helper.Log("Saving banner system");
                    __instance.newBannerSystem = true;
                    Main.helper.Log("Saving player creativeMode");
                    __instance.creativeMode = p.creativeMode;

                    //cmo options not used for saving or loading in multiplayer
                    /**for (int i = 0; i < p.cmoOptionsOn.Length; i++)
                    {
                        bool flag = p.cmoOptionsOn[i];
                        if (flag)
                        {
                            __instance.cmoOptions.Add((Player.CreativeOptions)i);
                        }
                    }**/

                    Main.helper.Log("Saving player upgrades");
                    __instance.GetType().GetField("upgrades", bindingFlags).SetValue(__instance, new List<Player.UpgradeType>());


                    Main.helper.Log("Saving player bannerIdx");
                    __instance.bannerIdx = p.PlayerLandmassOwner.bannerIdx;

                    Main.helper.Log("Saving player WorkersArray");
                    __instance.WorkersArray = new Villager.VillagerSaveData[p.Workers.Count];
                    for (int j = 0; j < p.Workers.Count; j++)
                    {
                        bool flag2 = p.Workers.data[j] != null;
                        if (flag2)
                        {
                            __instance.WorkersArray[j] = new Villager.VillagerSaveData().Pack(p.Workers.data[j]);
                        }
                    }

                    Main.helper.Log("Saving player HomelessData");
                    __instance.HomelessData = new List<Guid>();
                    for (int k = 0; k < p.Homeless.Count; k++)
                    {
                        __instance.HomelessData.Add(p.Homeless.data[k].guid);
                    }
                    __instance.structures = new List<Building.BuildingSaveData[]>();
                    __instance.subStructures = new List<Building.BuildingSaveData[]>();

                    Main.helper.Log("Saving player structures");
                    World.inst.ForEachTile(0, 0, World.inst.GridWidth, World.inst.GridHeight, delegate (int x, int z, Cell cell)
                    {
                        bool flag4 = cell.OccupyingStructure.Count > 0;
                        if (flag4)
                        {
                            List<Building.BuildingSaveData> occupyingStructureData = new List<Building.BuildingSaveData>();
                            for (int i3 = 0; i3 < cell.OccupyingStructure.Count; i3++)
                            {
                                var building = cell.OccupyingStructure[i3];
                                bool flag5 = Vector3.Distance(cell.OccupyingStructure[i3].transform.position.xz(), cell.Position.xz()) <= 1E-05f;
                                if (flag5 && building.TeamID() == p.PlayerLandmassOwner.teamId)
                                {
                                    occupyingStructureData.Add(new Building.BuildingSaveData().Pack(cell.OccupyingStructure[i3]));
                                }
                            }
                            bool flag6 = occupyingStructureData.Count > 0;
                            if (flag6)
                            {
                                __instance.structures.Add(occupyingStructureData.ToArray());
                            }
                        }
                        bool flag7 = cell.SubStructure.Count > 0;
                        if (flag7)
                        {
                            List<Building.BuildingSaveData> subStructureData = new List<Building.BuildingSaveData>();
                            for (int i4 = 0; i4 < cell.SubStructure.Count; i4++)
                            {
                                var building = cell.SubStructure[i4];
                                bool flag8 = Vector3.Distance(cell.SubStructure[i4].transform.position.xz(), cell.Position.xz()) <= 1E-05f;
                                if (flag8 && building.TeamID() == p.PlayerLandmassOwner.teamId)
                                {
                                    subStructureData.Add(new Building.BuildingSaveData().Pack(cell.SubStructure[i4]));
                                }
                            }
                            bool flag9 = subStructureData.Count > 0;
                            if (flag9)
                            {
                                __instance.subStructures.Add(subStructureData.ToArray());
                            }
                        }
                    });

                    Main.helper.Log($"Saving town happiness");
                    __instance.TownHappiness = p.KingdomHappiness;

                    Main.helper.Log($"Saving town happiness infos");
                    __instance.happinessInfos = p.GetType().GetField("landMassHappiness", bindingFlags).GetValue(p) as List<Player.HappinessInfo>;

                    Main.helper.Log($"Saving town integrity infos");
                    __instance.integrityInfos = p.GetType().GetField("landMassIntegrity", bindingFlags).GetValue(p) as List<Player.IntegrityInfo>;

                    Main.helper.Log($"Saving town landmass owner");
                    __instance.playerLandmassOwnerSaveData = new LandmassOwner.LandmassOwnerSaveData().Pack(p.PlayerLandmassOwner);

                    Main.helper.Log($"Saving town bDidFirstFire");
                    __instance.bDidFirstFire = (bool)p.GetType().GetField("bDidFirstFire", bindingFlags).GetValue(p);

                    bool flag3 = p.taxRates != null;
                    if (flag3)
                    {

                        Main.helper.Log($"Saving town tax rates");
                        __instance.TaxRates = new float[p.taxRates.Length];
                        Array.Copy(p.taxRates, __instance.TaxRates, p.taxRates.Length);
                    }

                    Main.helper.Log($"Saving difficulty");
                    __instance.Difficulty = p.difficulty;

                    Main.helper.Log($"Saving CurrYear");
                    __instance.CurrYear = p.CurrYear;

                    Main.helper.Log($"Saving timeAtFailHappiness");
                    __instance.timeAtFailHappiness = p.timeAtFailHappiness;

                    Main.helper.Log($"Saving happinessMods");
                    __instance.happinessMods = p.happinessMods;

                    Main.helper.Log($"Saving currConsumption");
                    __instance.currConsumptionList = p.currConsumption;

                    Main.helper.Log($"Saving lastConsumption");
                    __instance.lastConsumptionList = p.lastConsumption;

                    Main.helper.Log($"Saving currProduction");
                    __instance.currProductionList = p.currProduction;

                    Main.helper.Log($"Saving lastProduction");
                    __instance.lastProductionList = p.lastProduction;

                    Main.helper.Log($"Saving landMassNames");
                    __instance.landMassNames = new List<string>();
                    for (int l = 0; l < p.LandMassNames.Count; l++)
                    {
                        __instance.landMassNames.Add(p.LandMassNames[l]);
                    }

                    Main.helper.Log($"Saving JobPriorityOrder");
                    __instance.JobPriorityOrder = new int[p.JobPriorityOrder.Length][];
                    __instance.JobEnabledFlag = new bool[p.JobEnabledFlag.Length][];
                    for (int m = 0; m < p.JobPriorityOrder.Length; m++)
                    {
                        __instance.JobPriorityOrder[m] = new int[p.JobPriorityOrder[m].Length];
                        __instance.JobEnabledFlag[m] = new bool[p.JobEnabledFlag[m].Length];
                        Array.Copy(p.JobPriorityOrder[m], __instance.JobPriorityOrder[m], __instance.JobPriorityOrder[m].Length);
                        Array.Copy(p.JobEnabledFlag[m], __instance.JobEnabledFlag[m], __instance.JobEnabledFlag[m].Length);
                    }

                    Main.helper.Log($"Saving JobFilledAvailable");
                    __instance.JobFilledAvailable = new int[World.inst.NumLandMasses][];
                    __instance.JobCustomMaxEnabledFlag = new bool[World.inst.NumLandMasses][];
                    for (int lm = 0; lm < World.inst.NumLandMasses; lm++)
                    {
                        __instance.JobFilledAvailable[lm] = new int[38];
                        __instance.JobCustomMaxEnabledFlag[lm] = new bool[38];
                        for (int n = 0; n < 38; n++)
                        {
                            __instance.JobFilledAvailable[lm][n] = p.JobFilledAvailable.data[lm][n, 1];
                        }
                        Array.Copy(p.JobCustomMaxEnabledFlag[lm], __instance.JobCustomMaxEnabledFlag[lm], __instance.JobCustomMaxEnabledFlag[lm].Length);
                    }

                    Main.helper.Log($"Saving CanUseTools");
                    __instance.CanUseTools = new bool[p.CanUseTools.Length][];
                    for (int i2 = 0; i2 < p.CanUseTools.Length; i2++)
                    {
                        __instance.CanUseTools[i2] = new bool[p.CanUseTools[i2].Length];
                        Array.Copy(p.CanUseTools[i2], __instance.CanUseTools[i2], __instance.CanUseTools[i2].Length);
                    }

                    Main.helper.Log($"Saving usedCheats");
                    __instance.usedCheats = p.hasUsedCheats;

                    Main.helper.Log($"Saving nameForOldAgeDeath");
                    __instance.nameForOldAgeDeath = (string)p.GetType().GetField("nameForOldAgeDeath", bindingFlags).GetValue(p);

                    Main.helper.Log($"Saving deathsThisYear");
                    __instance.deathsThisYear = (int)p.GetType().GetField("deathsThisYear", bindingFlags).GetValue(p);

                    Main.helper.Log($"Saving poorHealthGracePeriod");
                    __instance.poorHealthGracePeriod = (float)p.GetType().GetField("poorHealthGracePeriod", bindingFlags).GetValue(p);

                    Main.helper.Log($"Saving dockOpenings");
                    __instance.dockOpenings = p.GetType().GetField("dockOpenings", bindingFlags).GetValue(p) as List<Player.DockOpening>;

                    Main.helper.Log($"Saving tourism");
                    __instance.tourism = p.tourism;


                    __result = __instance;

                    return false;
                }

                return true;
            }
        }

        /*[HarmonyPatch(typeof(Player.PlayerSaveData), "Unpack")]
        public class PlayerSaveDataUnpackHook
        {
            public static bool Prefix(Player.PlayerSaveData __instance, Player p, ref Player __result)
            {
                Main.helper.Log("Running patched player unpack method");
                if (KCClient.client.IsConnected)
                {
                    Main.helper.Log("Running patched unpack method");

                    var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;
                    Main.helper.Log("1");
                    Weather.inst.weatherTimeScale = 0f;
                    p.creativeMode = __instance.creativeMode;
                    p.ResetPerLandMassData();
                    Main.helper.Log("2");
                    bool flag = __instance.JobPriorityOrder != null && __instance.JobPriorityOrder[0].Length == ((int[])p.GetType().GetField("defaultPriorityOrder", bindingFlags).GetValue(p)).Length;
                    if (flag)
                    {
                        Main.helper.Log(__instance.JobPriorityOrder.Length.ToString());
                        Main.helper.Log(__instance.JobEnabledFlag.Length.ToString());
                        Main.helper.Log(p.JobEnabledFlag.Length.ToString());

                        for (int i = 0; i < __instance.JobPriorityOrder.Length; i++)
                        {
                            Array.Copy(__instance.JobPriorityOrder[i], p.JobPriorityOrder[i], __instance.JobPriorityOrder[i].Length);
                            Main.helper.Log("2.1");
                            Array.Copy(__instance.JobEnabledFlag[i], p.JobEnabledFlag[i], __instance.JobPriorityOrder[i].Length);
                            Main.helper.Log("2.2");
                        }
                    }
                    Main.helper.Log("3");
                    bool flag2 = __instance.JobFilledAvailable != null && __instance.JobFilledAvailable.Length == p.JobFilledAvailable.data.Length;
                    if (flag2)
                    {
                        for (int lm = 0; lm < World.inst.NumLandMasses; lm++)
                        {
                            bool flag3 = __instance.JobFilledAvailable[lm].Length != p.JobFilledAvailable.data[lm].Length / 2;
                            if (flag3)
                            {
                                break;
                            }
                            for (int j = 0; j < 38; j++)
                            {
                                p.JobFilledAvailable.data[lm][j, 0] = 0;
                                p.JobFilledAvailable.data[lm][j, 1] = __instance.JobFilledAvailable[lm][j];
                            }
                            Array.Copy(__instance.JobCustomMaxEnabledFlag[lm], p.JobCustomMaxEnabledFlag[lm], __instance.JobCustomMaxEnabledFlag[lm].Length);
                        }
                    }
                    Main.helper.Log("4");
                    // not saving creative info
                    p.ResetCreativeModeOptions();

                    p.KingdomHappiness = __instance.TownHappiness;
                    Main.helper.Log("5");
                    p.GetType().GetField("landMassHappiness", bindingFlags).SetValue(p, __instance.happinessInfos);
                    var landMassHappiness = p.GetType().GetField("landMassHappiness", bindingFlags).GetValue(p) as List<Player.HappinessInfo>;

                    bool flag5 = landMassHappiness == null;
                    if (flag5)
                    {
                        landMassHappiness = new List<Player.HappinessInfo>();
                    }
                    while (landMassHappiness.Count < World.inst.NumLandMasses)
                    {
                        landMassHappiness.Add(new Player.HappinessInfo());
                    }

                    Main.helper.Log("6");
                    p.GetType().GetField("landMassHealth", bindingFlags).SetValue(p, __instance.healthInfos);
                    var landMassHealth = p.GetType().GetField("landMassHealth", bindingFlags).GetValue(p) as List<Player.HealthInfo>;

                    bool flag6 = landMassHealth == null;
                    if (flag6)
                    {
                        landMassHealth = new List<Player.HealthInfo>();
                    }
                    while (landMassHealth.Count < World.inst.NumLandMasses)
                    {
                        landMassHealth.Add(new Player.HealthInfo());
                    }

                    Main.helper.Log("7");

                    p.GetType().GetField("landMassIntegrity", bindingFlags).SetValue(p, __instance.integrityInfos);
                    var landMassIntegrity = p.GetType().GetField("landMassIntegrity", bindingFlags).GetValue(p) as List<Player.IntegrityInfo>;

                    bool flag7 = landMassIntegrity == null;
                    if (flag7)
                    {
                        landMassIntegrity = new List<Player.IntegrityInfo>();
                    }
                    while (landMassIntegrity.Count < World.inst.NumLandMasses)
                    {
                        landMassIntegrity.Add(new Player.IntegrityInfo());
                    }
                    Main.helper.Log("8");
                    p.GetType().GetField("bDidFirstFire", bindingFlags).SetValue(p, __instance.bDidFirstFire);

                    bool flag8 = __instance.TaxRates == null;
                    if (flag8)
                    {
                        p.taxRates = new float[World.inst.NumLandMasses];
                        int l = 0;
                        int m = World.inst.NumLandMasses;
                        while (l < m)
                        {
                            p.taxRates[l] = (float)__instance.TaxRate;
                            l++;
                        }
                    }
                    else
                    {
                        p.taxRates = new float[__instance.TaxRates.Length];
                        Array.Copy(__instance.TaxRates, p.taxRates, __instance.TaxRates.Length);
                    }
                    p.difficulty = __instance.Difficulty;
                    p.CurrYear = __instance.CurrYear;
                    p.timeAtFailHappiness = __instance.timeAtFailHappiness;
                    p.currConsumption = __instance.currConsumptionList;
                    Main.helper.Log("9");
                    while (p.currConsumption.Count < World.inst.NumLandMasses)
                    {
                        p.currConsumption.Add(new Player.Consumption());
                    }
                    p.lastConsumption = __instance.lastConsumptionList;
                    while (p.lastConsumption.Count < World.inst.NumLandMasses)
                    {
                        p.lastConsumption.Add(new Player.Consumption());
                    }
                    p.currProduction = __instance.currProductionList;
                    while (p.currProduction.Count < World.inst.NumLandMasses)
                    {
                        p.currProduction.Add(new Player.Production());
                    }
                    p.lastProduction = __instance.lastProductionList;
                    while (p.lastProduction.Count < World.inst.NumLandMasses)
                    {
                        p.lastProduction.Add(new Player.Production());
                    }
                    p.happinessMods = __instance.happinessMods;
                    Main.helper.Log("10");
                    bool flag9 = p.happinessMods == null;
                    if (flag9)
                    {
                        p.happinessMods = new List<Player.HappinessMod>();
                    }
                    Main.helper.Log("11");
                    bool flag10 = __instance.landMassNames == null;
                    if (flag10)
                    {
                        p.ResetLandMassNames();
                    }
                    else
                    {
                        p.LandMassNames.Clear();
                        for (int n = 0; n < __instance.landMassNames.Count; n++)
                        {
                            p.LandMassNames.Add(__instance.landMassNames[n]);
                        }
                    }
                    Main.helper.Log("12");
                    bool flag11 = __instance.playerLandmassOwnerSaveData != null;
                    if (flag11)
                    {
                        __instance.playerLandmassOwnerSaveData.Unpack(p.PlayerLandmassOwner);
                    }
                    else
                    {

                        var Resources = (ResourceAmount)__instance.GetType().GetField("Resources").GetValue(__instance);

                        p.PlayerLandmassOwner.Gold = Resources.Get(FreeResourceType.Gold);
                        for (int i2 = 0; i2 < __instance.structures.Count; i2++)
                        {
                            Building.BuildingSaveData[] occupyingStructureData = __instance.structures[i2];
                            for (int h = 0; h < occupyingStructureData.Length; h++)
                            {
                                int lIdx = World.inst.GetCellData(occupyingStructureData[h].globalPosition).landMassIdx;
                                bool flag12 = !p.PlayerLandmassOwner.OwnsLandMass(lIdx);
                                if (flag12)
                                {
                                    p.PlayerLandmassOwner.TakeOwnership(lIdx);
                                }
                            }
                        }
                    }
                    Main.helper.Log("13");
                    bool flag13 = __instance.bannerIdx != -1;
                    if (flag13)
                    {
                        p.SetIndexedBanner(__instance.bannerIdx);
                    }
                    bool flag14 = !__instance.newBannerSystem;
                    Main.helper.Log("14");
                    if (flag14)
                    {
                        p.SetIndexedBanner(5);
                        p.SetCustomBannerTexture(World.inst.liverySets[__instance.bannerIdx].bannerMaterial.mainTexture as Texture2D);
                    }
                    bool flag15 = p.PlayerLandmassOwner.Gold < 0;
                    if (flag15)
                    {
                        p.PlayerLandmassOwner.Gold = 0;
                    }

                    Main.helper.Log("15");
                    var upgrades = __instance.GetType().GetField("upgrades", bindingFlags).GetValue(__instance) as List<Player.UpgradeType>;
                    for (int i3 = 0; i3 < upgrades.Count; i3++)
                    {
                        p.PlayerLandmassOwner.AddUpgrade(upgrades[i3]);
                    }
                    p.Workers.Clear();
                    p.Homeless.Clear();

                    Main.helper.Log("16");
                    Main.helper.Log($"Loading {__instance.WorkersArray.Length} workers from workers array for {p.PlayerLandmassOwner.teamId}");
                    bool flag16 = __instance.WorkersArray != null;
                    if (flag16)
                    {

                        Main.helper.Log("17");
                        for (int i4 = 0; i4 < __instance.WorkersArray.Length; i4++)
                        {
                            Villager person = Villager.CreateVillager();
                            person.Pos = __instance.WorkersArray[i4].pos;
                            __instance.WorkersArray[i4].Unpack(person);
                            p.Workers.Add(person);
                        }
                    }
                    else
                    {

                        Main.helper.Log("18");
                        Main.helper.Log($"Loading {__instance.Workers.Count} workers for {p.PlayerLandmassOwner.teamId}");
                        for (int i5 = 0; i5 < __instance.Workers.Count; i5++)
                        {
                            Villager person2 = Villager.CreateVillager();
                            person2.Pos = __instance.Workers[i5].pos;
                            __instance.Workers[i5].Unpack(person2);
                            p.Workers.Add(person2);
                        }
                    }

                    Main.helper.Log("19");
                    for (int i6 = 0; i6 < __instance.HomelessData.Count; i6++)
                    {
                        Villager worker = p.GetWorker(__instance.HomelessData[i6]);
                        bool flag17 = worker != null;
                        if (flag17)
                        {
                            p.Homeless.Add(worker);
                        }
                    }
                    Main.helper.Log("20");

                    /*List<Player.PlayerSaveData.BuildingLoadHelper> buildingsToPlace = new List<Player.PlayerSaveData.BuildingLoadHelper>();
                    for (int i7 = 0; i7 < __instance.structures.Count; i7++)
                    {
                        Building.BuildingSaveData[] occupyingStructureData2 = __instance.structures[i7];
                        for (int h2 = 0; h2 < occupyingStructureData2.Length; h2++)
                        {
                            Building building = __instance.ProcessBuilding(occupyingStructureData2[h2], p);
                            bool flag18 = building != null;
                            if (flag18)
                            {
                                buildingsToPlace.Add(new Player.PlayerSaveData.BuildingLoadHelper(building, occupyingStructureData2[h2], h2));
                            }
                        }
                    }
                    for (int i8 = 0; i8 < __instance.subStructures.Count; i8++)
                    {
                        Building.BuildingSaveData[] occupyingStructureData3 = __instance.subStructures[i8];
                        for (int h3 = 0; h3 < occupyingStructureData3.Length; h3++)
                        {
                            Building building2 = __instance.ProcessBuilding(occupyingStructureData3[h3], p);
                            bool flag19 = building2 != null;
                            if (flag19)
                            {
                                buildingsToPlace.Add(new Player.PlayerSaveData.BuildingLoadHelper(building2, occupyingStructureData3[h3], h3 - 1000));
                            }
                        }
                    }
                    foreach (Player.PlayerSaveData.BuildingLoadHelper buildingHelper in from x in buildingsToPlace
                                                                                        orderby x.priority
                                                                                        select x)
                    {
                        World.inst.PlaceFromLoad(buildingHelper.building);
                        buildingHelper.buildingSaveData.UnpackStage2(buildingHelper.building);
                    }
                    for (int i9 = 0; i9 < p.Workers.Count; i9++)
                    {
                        bool flag20 = p.Workers.data[i9].Residence == null;
                        if (flag20)
                        {
                            bool flag21 = !p.Homeless.Contains(p.Workers.data[i9]);
                            if (flag21)
                            {
                                p.Homeless.Add(p.Workers.data[i9]);
                                Debug.LogError("worker with null residence not in homeless saved data...");
                            }
                        }
                    }
                    for (int i10 = 0; i10 < buildingsToPlace.Count; i10++)
                    {
                        Player.PlayerSaveData.BuildingLoadHelper buildingHelper2 = buildingsToPlace[i10];
                        Road roadComp = buildingHelper2.building.GetComponent<Road>();
                        bool flag22 = roadComp != null;
                        if (flag22)
                        {
                            roadComp.UpdateRotation();
                        }
                        bool flag23 = buildingHelper2.building.uniqueNameHash == Player.PlayerSaveData.aqueductHash;
                        if (flag23)
                        {
                            buildingHelper2.building.GetComponent<Aqueduct>().UpdateRotation();
                        }
                    }
                    Cell[] cellData = World.inst.GetCellsData();
                    for (int i11 = 0; i11 < cellData.Length; i11++)
                    {
                        List<Building> structures = cellData[i11].OccupyingStructure;
                        for (int j2 = 0; j2 < structures.Count; j2++)
                        {
                            bool flag24 = structures[j2].categoryHash == Player.PlayerSaveData.castleblockHash;
                            if (flag24)
                            {
                                structures[j2].GetComponent<CastleBlock>().UpdateStackPostLoad();
                                break;
                            }
                        }
                    }
                    bool flag25 = Player.inst.keep != null;
                    if (flag25)
                    {
                        p.buildingContainer.BroadcastMessage("OnAnyBuildingAdded", Player.inst.keep.GetComponent<Building>(), SendMessageOptions.DontRequireReceiver);
                    }
                    World.inst.SetupInitialPathCosts();
                    Player.inst.irrigation.UpdateIrrigation();
                    Player.inst.CalcMaxResources(null, -1);
                    bool flag26 = __instance.CanUseTools != null;
                    if (flag26)
                    {
                        int i12 = 0;
                        while (i12 < __instance.CanUseTools.Length && i12 < p.CanUseTools.Length)
                        {
                            Array.Copy(__instance.CanUseTools[i12], p.CanUseTools[i12], __instance.CanUseTools[i12].Length);
                            i12++;
                        }
                    }
                    ToolInfo.inst.SetTogglesFromPlayerData();
                    p.hasUsedCheats = __instance.usedCheats;
                    p.checkedForceCreative = false;
                    p.nameForOldAgeDeath = __instance.nameForOldAgeDeath;
                    p.deathsThisYear = __instance.deathsThisYear;
                    World.inst.RebuildVillagerGrid();
                    ArrayExt<Building> keepers = Player.inst.GetBuildingList(World.cemeteryKeeperHash);
                    for (int i13 = 0; i13 < keepers.Count; i13++)
                    {
                        keepers.data[i13].GetComponent<CemeteryKeeper>().RebuildPathData();
                    }
                    p.ChangeHazardPayActive(p.PlayerLandmassOwner.hazardPay, false);
                    p.poorHealthGracePeriod = __instance.poorHealthGracePeriod;
                    p.UpdateFocusedLandmass();
                    PopulationUI.inst.regionName.RefreshRegionName();
                    p.dockOpenings = __instance.dockOpenings;
                    bool flag27 = p.dockOpenings == null;
                    if (flag27)
                    {
                        p.dockOpenings = new List<Player.DockOpening>();
                    }
                    p.IntegrityTimer.ForceExpire();
                    bool flag28 = __instance.tourism != null;
                    if (flag28)
                    {
                        p.tourism = __instance.tourism;
                    }
                    else
                    {
                        p.tourism = new Player.TourismInfo();
                    }


                    __result = p;

                    return false;
                }

                return true;
            }
        }*/

        #endregion

        /**
         * 
         * Find all Player.inst references and reconstruct method with references to client planyer
         * 
         * Instantiating main player object and setting landmass teamid in KCPLayer
         * 
         * E.G instead of Player.inst, it should be Main.kCPlayers[Client].player for example, and the rest of the code is the same
         * 
         * Prefix that sets Player.inst to the right client instance and then calls that instances method?
         * 
         */

        [HarmonyPatch]
        public class PlayerReferencePatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                Assembly assembly = typeof(Player).Assembly;

                Type[] types = new Type[] { typeof(Player)/*, typeof(World), typeof(LandmassOwner), typeof(Keep), typeof(Villager), typeof(DragonSpawn), typeof(DragonController), typeof(Dragon)*/ };

                var methodsInNamespace = types
                    .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsAbstract))
                    .ToList();

                helper.Log("Methods in namespace: " + methodsInNamespace.Count);

                return methodsInNamespace.ToArray().Cast<MethodBase>();
            }

            static IEnumerable<CodeInstruction> Transpiler(MethodBase method, IEnumerable<CodeInstruction> instructions)
            {
                int PlayerInstCount = 0;

                var codes = new List<CodeInstruction>(instructions);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand.ToString() == "Player inst")
                    {
                        PlayerInstCount++;

                        codes[i].opcode = (OpCodes.Ldarg_0); // Replace all instance methods static ref with "this" instead of Player.inst

                        // Replace ldsfld Player::inst with the sequence to load from Main.kCPlayers
                        // Step 1: Load Main.kCPlayers onto the evaluation stack.
                        //codes[i] = new CodeInstruction(OpCodes.Ldsfld, typeof(Main).GetField("kCPlayers"));

                        // Step 2: Load the value of Main.PlayerSteamID onto the evaluation stack as the key
                        //codes.Insert(++i, new CodeInstruction(OpCodes.Ldsfld, typeof(Main).GetField("PlayerSteamID")));

                        // Step 3: Call Dictionary<TKey, TValue>.get_Item(TKey key) to get the Player instance.
                        //codes.Insert(++i, new CodeInstruction(OpCodes.Callvirt, typeof(Dictionary<string, KCPlayer>).GetMethod("get_Item")));

                        // Now, access the 'inst' field of the fetched Player instance, if necessary.
                        //codes.Insert(++i, new CodeInstruction(OpCodes.Ldfld, typeof(KCPlayer).GetField("inst")));
                    }
                }

                if (PlayerInstCount > 0)
                    Main.helper.Log($"Found {PlayerInstCount} static Player.inst references in {method.Name}");

                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch]
        public class BuildingPlayerReferencePatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                Assembly assembly = typeof(Building).Assembly;

                Type[] types = new Type[] { typeof(Building) };

                var methodsInNamespace = types
                    .SelectMany(t => t.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsAbstract))
                    .ToList();

                helper.Log("Methods in namespace: " + methodsInNamespace.Count);

                return methodsInNamespace.ToArray().Cast<MethodBase>();
            }

            static IEnumerable<CodeInstruction> Transpiler(MethodBase method, IEnumerable<CodeInstruction> instructions)
            {
                int PlayerInstCount = 0;

                var codes = new List<CodeInstruction>(instructions);
                MethodInfo getPlayerByBuildingMethodInfo = typeof(Main).GetMethod("GetPlayerByBuilding", BindingFlags.Static | BindingFlags.Public);

                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand.ToString() == "Player inst")
                    {
                        PlayerInstCount++;

                        // Check if the current instruction is ldsfld Player.inst
                        if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand.ToString().Contains("Player inst"))
                        {
                            // Replace the instruction sequence
                            // Step 1: Load 'this' for the Building instance
                            codes[i].opcode = OpCodes.Ldarg_0;

                            // Step 2: Call GetPlayerByBuilding(Building instance) static method in Main
                            var callTeamID = new CodeInstruction(OpCodes.Call, getPlayerByBuildingMethodInfo);
                            codes.Insert(++i, callTeamID);
                        }
                    }
                }

                if (PlayerInstCount > 0)
                    Main.helper.Log($"Found {PlayerInstCount} static building Player.inst references in {method.Name}");

                return codes.AsEnumerable();
            }
        }


        [HarmonyPatch]
        public class PlayerPatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var meth = typeof(Player).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                return meth.Cast<MethodBase>();
            }

            public static bool Prefix(MethodBase __originalMethod, Player __instance)
            {
                if (__originalMethod.Name.Equals("Awake") && (KCServer.IsRunning || KCClient.client.IsConnected))
                {
                    helper.Log("Awake run on player instance while server is running");

                    return false;
                }

                if (__originalMethod.Name.Equals("Awake") && __instance.gameObject.name.Contains("Client Player"))
                {
                    helper.Log("Awake run on client instance");
                    try
                    {
                        //___defaultEnabledFlags = new bool[38];
                        //for (int i = 0; i < ___defaultEnabledFlags.Length; i++)
                        //{
                        //    ___defaultEnabledFlags[i] = true;
                        //}
                        //__instance.PlayerLandmassOwner = __instance.gameObject.AddComponent<LandmassOwner>();



                        //helper.Log(__instance.PlayerLandmassOwner.ToString());
                    }
                    catch (Exception e)
                    {
                        helper.Log(e.ToString());
                        helper.Log(e.Message);
                        helper.Log(e.StackTrace);
                    }
                    return false;
                }

                if (__originalMethod.Name.Equals("Update") && __instance.gameObject.name.Contains("Client Player"))
                {
                    //helper.Log("Update run on client instance");
                    try
                    {
                        //___defaultEnabledFlags = new bool[38];
                        //for (int i = 0; i < ___defaultEnabledFlags.Length; i++)
                        //{
                        //    ___defaultEnabledFlags[i] = true;
                        //}
                        //__instance.PlayerLandmassOwner = __instance.gameObject.AddComponent<LandmassOwner>();



                        //helper.Log(__instance.PlayerLandmassOwner.ToString());
                    }
                    catch (Exception e)
                    {
                        helper.Log(e.ToString());
                        helper.Log(e.Message);
                        helper.Log(e.StackTrace);
                    }
                    return false;
                }

                if (__originalMethod.Name.Equals("Update"))
                {
                    //helper.Log($"Update called for: {__instance.gameObject.name}");

                    try
                    {
                        if (KCClient.client.IsConnected && !__instance.gameObject.name.Contains("Client Player"))
                        {
                            StateObserver.RegisterObserver(__instance, new string[] {
                                "bannerIdx", "kingdomHappiness", "landMassHappiness", "landMassIntegrity", "bDidFirstFire", "CurrYear",
                                "timeAtFailHappiness", "hasUsedCheats", "nameForOldAgeDeath", "deathsThisYear", /*"poorHealthGracePeriod",*/
                            });

                            //StateObserver.Update(__instance);
                        }
                    }
                    catch (Exception e)
                    {
                        helper.Log(e.ToString());
                        helper.Log(e.Message);
                        helper.Log(e.StackTrace);
                    }
                    return true;
                }

                return true;
            }

            public static void Postfix(MethodBase __originalMethod, Player __instance)
            {
                if (__originalMethod.Name.Equals("Update"))
                {
                    //helper.Log($"Update called for: {__instance.gameObject.name} in POSTFIX");


                    //helper.Log("CHECKING ALL COMPONENTS IN UPDATE: ");
                    //Component[] components = __instance.gameObject.GetComponents<Component>();

                    //foreach (Component component in components)
                    //{
                    //    helper.Log("--- " + component.GetType().kingdomName);
                    //}
                }
            }
        }

        #region "Unity Log Hooks"

        [HarmonyPatch(typeof(UnityEngine.Debug), "Log", new Type[] { typeof(object) })]
        public class DebugLogPatch
        {
            public static void Prefix(object message)
            {
                if (Main.kCPlayers.Values.Any((p) => message.ToString().StartsWith(p.inst.PlayerLandmassOwner.teamId.ToString())))
                    return;

                //Main.helper.Log($"UNITY 3D DEBUG LOG");
                //Main.helper.Log(message.ToString());
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug), "Log", new Type[] { typeof(object), typeof(UnityEngine.Object) })]
        public class DebugLogCPatch
        {
            public static void Prefix(object message, UnityEngine.Object context)
            {
                if (Main.kCPlayers.Values.Any((p) => message.ToString().StartsWith(p.inst.PlayerLandmassOwner.teamId.ToString())))
                    return;

                //Main.helper.Log($"UNITY 3D DEBUG LOG");
                //Main.helper.Log(context.ToString());
                //Main.helper.Log(message.ToString());
            }
        }


        [HarmonyPatch(typeof(UnityEngine.Debug), "LogError", new Type[] { typeof(object) })]
        public class DebugLogErrorPatch
        {
            public static void Prefix(object message)
            {
                if (message.ToString().StartsWith(Player.inst?.PlayerLandmassOwner?.teamId.ToString() ?? ""))
                    return;

                //Main.helper.Log($"UNITY 3D DEBUG LOG ERROR");
                //Main.helper.Log(message.ToString());
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug), "LogError", new Type[] { typeof(object), typeof(UnityEngine.Object) })]
        public class DebugLogErrorCPatch
        {
            public static void Prefix(object message, UnityEngine.Object context)
            {
                if (message.ToString().StartsWith(Player.inst?.PlayerLandmassOwner?.teamId.ToString() ?? ""))
                    return;

                //Main.helper.Log($"UNITY 3D DEBUG LOG ERROR");
                //Main.helper.Log(context.ToString());
                //Main.helper.Log(message.ToString());
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug), "LogException", new Type[] { typeof(Exception) })]
        public class DebugLogExceptionPatch
        {
            public static void Prefix(Exception exception)
            {
                //Main.helper.Log($"UNITY 3D DEBUG LOG EXCEPTION");
                //Main.helper.Log(exception.Message);
                //Main.helper.Log(exception.StackTrace);
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug), "LogException", new Type[] { typeof(Exception), typeof(UnityEngine.Object) })]
        public class DebugLogExceptionCPatch
        {
            public static void Prefix(Exception exception, UnityEngine.Object context)
            {
                //Main.helper.Log($"UNITY 3D DEBUG LOG EXCEPTION");
                //Main.helper.Log(exception.Message);
                //Main.helper.Log(exception.StackTrace);
            }
        }


        [HarmonyPatch(typeof(UnityEngine.Debug), "LogWarning", new Type[] { typeof(object) })]
        public class DebugLogWarningPatch
        {
            public static void Prefix(object message)
            {
                if (message.ToString().Contains("Failed to send 928 byte"))
                    return;


                //Main.helper.Log($"UNITY 3D DEBUG LOG WARNING");
                //Main.helper.Log(message.ToString());
            }
        }

        [HarmonyPatch(typeof(UnityEngine.Debug), "LogWarning", new Type[] { typeof(object), typeof(UnityEngine.Object) })]
        public class DebugLogWarningCPatch
        {
            public static void Prefix(object message, UnityEngine.Object context)
            {
                if (Main.kCPlayers.Values.Any((p) => message.ToString().StartsWith(p.inst.PlayerLandmassOwner.teamId.ToString())))
                    return;

                //Main.helper.Log($"UNITY 3D DEBUG LOG WARNING");
                //Main.helper.Log(context.ToString());
                //Main.helper.Log(message.ToString());
            }
        }

        #endregion
    }

    public class DetailedTransformData
    {
        public string name;
        public string path;
        public Vector3 position;
        public Vector3 localPosition;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
        public Vector3 lossyScale;
        public List<DetailedTransformData> children;
        public List<string> components;

        public DetailedTransformData(Transform transform, string parentPath = "")
        {
            name = transform.name;
            path = parentPath == "" ? name : parentPath + "/" + name;
            position = transform.position;
            localPosition = transform.localPosition;
            rotation = transform.rotation;
            localRotation = transform.localRotation;
            eulerAngles = transform.eulerAngles;
            localEulerAngles = transform.localEulerAngles;
            localScale = transform.localScale;
            lossyScale = transform.lossyScale;
            children = new List<DetailedTransformData>();

            components = transform.GetComponents<Component>().Select(c => c.GetType().ToString()).ToList();

            foreach (Transform child in transform)
            {
                children.Add(new DetailedTransformData(child, path));
            }
        }
    }

}
