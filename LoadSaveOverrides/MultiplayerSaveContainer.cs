using Assets.Code;
using Riptide;
using Riptide.Transports;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KCM.LoadSaveOverrides
{
    [Serializable]
    public class MultiplayerSaveContainer : LoadSaveContainer
    {
        public Dictionary<string, Player.PlayerSaveData> players = new Dictionary<string, Player.PlayerSaveData>();
        public Dictionary<string, string> kingdomNames = new Dictionary<string, string>();

        public new MultiplayerSaveContainer Pack(object obj)
        {
            this.CameraSaveData = new Cam.CamSaveData().Pack(Cam.inst);
            this.TownNameSaveData = new TownNameUI.TownNameSaveData().Pack(TownNameUI.inst);

            Main.helper.Log($"Saving data for {Main.kCPlayers.Count} ({KCServer.server.ClientCount}) players.");

            //this.PlayerSaveData = new PlayerSaveDataOverride().Pack(Player.inst);
            foreach (var player in Main.kCPlayers.Values)
            {
                try
                {
                    if (player == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(player.steamId))
                    {
                        Main.helper.Log($"Skipping save for player with missing steamId (name={player.name ?? string.Empty})");
                        continue;
                    }

                    if (player.inst == null)
                    {
                        Main.helper.Log($"Skipping save for player {player.name ?? string.Empty} ({player.steamId}) because Player.inst is null");
                        continue;
                    }

                    Main.helper.Log($"Attempting to pack data for: {player.name} ({player.steamId})");
                    string playerGoName = (player.inst.gameObject != null) ? player.inst.gameObject.name : string.Empty;
                    Main.helper.Log($"Player object: {player.inst} {playerGoName}");

                    this.players[player.steamId] = new Player.PlayerSaveData().Pack(player.inst);
                    kingdomNames[player.steamId] = player.kingdomName ?? " ";

                    Main.helper.Log($"{players[player.steamId] == null}");
                }
                catch (Exception ex)
                {
                    string steamId = (player != null && player.steamId != null) ? player.steamId : string.Empty;
                    string name = (player != null && player.name != null) ? player.name : string.Empty;
                    Main.helper.Log($"Error packing player data for save (steamId={steamId}, name={name})");
                    Main.helper.Log(ex.ToString());
                }
            }

            this.WorldSaveData = new World.WorldSaveData().Pack(World.inst);
            this.FishSystemSaveData = new FishSystem.FishSystemSaveData().Pack(FishSystem.inst);
            this.JobSystemSaveData = new JobSystem.JobSystemSaveData().Pack(JobSystem.inst);
            this.FreeResourceManagerSaveData = new FreeResourceManager.FreeResourceManagerSaveData().Pack(FreeResourceManager.inst);
            this.WeatherSaveData = new Weather.WeatherSaveData().Pack(Weather.inst);
            this.FireManagerSaveData = new FireManager.FireManagerSaveData().Pack(FireManager.inst);
            this.DragonSpawnSaveData = new DragonSpawn.DragonSpawnSaveData().Pack(DragonSpawn.inst);
            this.UnitSystemSaveData = new UnitSystem.UnitSystemSaveData().Pack(UnitSystem.inst);
            this.RaidSystemSaveData2 = new RaiderSystem.RaiderSystemSaveData2().Pack(RaiderSystem.inst);

            if (ShipSystem.inst != null)
            {
                try
                {
                    this.ShipSystemSaveData = new ShipSystem.ShipSystemSaveData().Pack(ShipSystem.inst);
                }
                catch (Exception ex)
                {
                    Main.helper.Log("Error packing ShipSystem for save; skipping ShipSystemSaveData.");
                    Main.helper.Log(ex.ToString());
                    this.ShipSystemSaveData = null;
                }
            }
            else
            {
                this.ShipSystemSaveData = null;
            }

            this.AIBrainsSaveData = new AIBrainsContainer.SaveData().Pack(AIBrainsContainer.inst);
            this.SiegeMonsterSaveData = new SiegeMonster.SiegeMonsterSaveData().Pack(null);
            this.CartSystemSaveData = new CartSystem.CartSystemSaveData().Pack(CartSystem.inst);
            this.SiegeCatapultSystemSaveData = new SiegeCatapultSystem.SiegeCatapultSystemSaveData().Pack(SiegeCatapultSystem.inst);
            this.OrdersManagerSaveData = new OrdersManager.OrdersManagerSaveData().Pack(OrdersManager.inst);
            this.CustomSaveData = LoadSave.CustomSaveData_DontAccessDirectly;

            return this;
        }

        public override object Unpack(object obj)
        {
            //original Player reset was up here
            foreach (var kvp in players)
            {

                KCPlayer player;

                if (!Main.kCPlayers.TryGetValue(kvp.Key, out player))
                {
                    player = new KCPlayer("", 50, kvp.Key);
                    player.kingdomName = kingdomNames[kvp.Key];

                    Main.kCPlayers.Add(kvp.Key, player);
                }
            }

            foreach (var player in Main.kCPlayers.Values)
                player.inst.Reset();


            AIBrainsContainer.inst.ClearAIs();
            this.CameraSaveData.Unpack(Cam.inst);
            this.WorldSaveData.Unpack(World.inst);

            bool flag = this.FishSystemSaveData != null;
            if (flag)
            {
                this.FishSystemSaveData.Unpack(FishSystem.inst);
            }
            this.TownNameSaveData.Unpack(TownNameUI.inst);


            //TownNameUI.inst.townName = kingdomNames[Main.PlayerSteamID];
            TownNameUI.inst.SetTownName(kingdomNames[Main.PlayerSteamID]);

            Main.helper.Log("Unpacking player data");

            Player.PlayerSaveData clientPlayerData = null;

            foreach (var kvp in players)
            {
                if (kvp.Key == SteamUser.GetSteamID().ToString())
                {
                    Main.helper.Log("Found current client player data. ID: " + SteamUser.GetSteamID().ToString());

                    clientPlayerData = kvp.Value;
                }
                else
                { // Maybe ??
                    Main.helper.Log("Loading player data: " + kvp.Key);


                    KCPlayer player;

                    if (!Main.kCPlayers.TryGetValue(kvp.Key, out player))
                    {
                        player = new KCPlayer("", 50, kvp.Key);
                        Main.kCPlayers.Add(kvp.Key, player);
                    }

                    Player oldPlayer = Player.inst;
                    Player.inst = player.inst;
                    Main.helper.Log($"Number of landmasses: {World.inst.NumLandMasses}");

                    //Reset was here before unpack
                    kvp.Value.Unpack(player.inst);

                    Player.inst = oldPlayer;


                    player.banner = player.inst.PlayerLandmassOwner.bannerIdx;
                    player.kingdomName = TownNameUI.inst.townName;
                }
            }

            clientPlayerData.Unpack(Player.inst); // Unpack the current client player last so that loading of villagers works correctly.

            Main.helper.Log("unpacked player data");
            Main.helper.Log("Setting banner and name");

            var client = Main.kCPlayers[SteamUser.GetSteamID().ToString()];


            client.banner = Player.inst.PlayerLandmassOwner.bannerIdx;
            client.kingdomName = TownNameUI.inst.townName;

            Main.helper.Log("Finished unpacking player data");

            /*
             *  Not even going to bother fixing AI brains save data yet, not in short-term roadmap
             */

            /*bool flag2 = this.AIBrainsSaveData != null;
            if (flag2)
            {
                this.AIBrainsSaveData.UnpackPrePlayer(AIBrainsContainer.inst);
            }*/

            Main.helper.Log("Unpacking free resource manager");
            this.FreeResourceManagerSaveData.Unpack(FreeResourceManager.inst);
            Main.helper.Log("Unpacking job system");
            this.JobSystemSaveData.Unpack(JobSystem.inst);
            Main.helper.Log("Unpacking weather");
            this.WeatherSaveData.Unpack(Weather.inst);
            Main.helper.Log("Unpacking fire manager");
            this.FireManagerSaveData.Unpack(FireManager.inst);
            Main.helper.Log("Unpacking dragon spawn");
            this.DragonSpawnSaveData.Unpack(DragonSpawn.inst);
            Main.helper.Log("Unpacking unit system");
            bool flag3 = this.UnitSystemSaveData != null;
            if (flag3)
            {
                this.UnitSystemSaveData.Unpack(UnitSystem.inst);
            }
            Main.helper.Log("Unpacking siege monster");
            bool flag4 = this.SiegeMonsterSaveData != null;
            if (flag4)
            {
                this.SiegeMonsterSaveData.Unpack(null);
            }
            Main.helper.Log("Unpacking siege catapult system");
            bool flag5 = this.SiegeCatapultSystemSaveData != null;
            if (flag5)
            {
                this.SiegeCatapultSystemSaveData.Unpack(SiegeCatapultSystem.inst);
            }
            Main.helper.Log("Unpacking ship system");
            bool flag6 = this.ShipSystemSaveData != null;
            if (flag6)
            {
                this.ShipSystemSaveData.Unpack(ShipSystem.inst);
            }
            Main.helper.Log("Unpacking cart system");
            bool flag7 = this.CartSystemSaveData != null;
            if (flag7)
            {
                this.CartSystemSaveData.Unpack(CartSystem.inst);
            }
            Main.helper.Log("Unpacking raid system");
            bool flag8 = this.RaidSystemSaveData2 != null;
            if (flag8)
            {
                this.RaidSystemSaveData2.Unpack(RaiderSystem.inst);
            }
            Main.helper.Log("Unpacking orders manager");
            bool flag9 = this.OrdersManagerSaveData != null;
            if (flag9)
            {
                this.OrdersManagerSaveData.Unpack(OrdersManager.inst);
            }
            Main.helper.Log("Unpacking AI brains");
            bool flag10 = this.AIBrainsSaveData != null;
            if (flag10)
            {
                this.AIBrainsSaveData.Unpack(AIBrainsContainer.inst);
            }
            Main.helper.Log("Unpacking custom save data");
            bool flag11 = this.CustomSaveData != null;
            if (flag11)
            {
                LoadSave.CustomSaveData_DontAccessDirectly = this.CustomSaveData;
            }
            Main.helper.Log("Unpacking done");

            try
            {
                Main.helper.Log("Post-load: rebuilding path costs + villager grid");
                try { World.inst.SetupInitialPathCosts(); } catch (Exception e) { Main.helper.Log(e.ToString()); }
                try { World.inst.RebuildVillagerGrid(); } catch (Exception e) { Main.helper.Log(e.ToString()); }
                try { Player.inst.irrigation.UpdateIrrigation(); } catch (Exception e) { Main.helper.Log(e.ToString()); }
                try { Player.inst.CalcMaxResources(null, -1); } catch (Exception e) { Main.helper.Log(e.ToString()); }
            }
            catch (Exception e)
            {
                Main.helper.Log("Post-load rebuild failed");
                Main.helper.Log(e.ToString());
            }


            World.inst.UpscaleFeatures();
            Player.inst.RefreshVisibility(true);
            for (int i = 0; i < Player.inst.Buildings.Count; i++)
            {
                Player.inst.Buildings.data[i].UpdateMaterialSelection();
            }

            // Player.inst.loadTickDelay = 1;
            Type playerType = typeof(Player);
            FieldInfo loadTickDelayField = playerType.GetField("loadTickDelay", BindingFlags.Instance | BindingFlags.NonPublic);
            if (loadTickDelayField != null)
            {
                loadTickDelayField.SetValue(Player.inst, 1);
            }

            // UnitSystem.inst.loadTickDelay = 1;
            Type unitSystemType = typeof(UnitSystem);
            loadTickDelayField = unitSystemType.GetField("loadTickDelay", BindingFlags.Instance | BindingFlags.NonPublic);
            if (loadTickDelayField != null)
            {
                loadTickDelayField.SetValue(UnitSystem.inst, 1);
            }

            // JobSystem.inst.loadTickDelay = 1;
            Type jobSystemType = typeof(JobSystem);
            loadTickDelayField = jobSystemType.GetField("loadTickDelay", BindingFlags.Instance | BindingFlags.NonPublic);
            if (loadTickDelayField != null)
            {
                loadTickDelayField.SetValue(JobSystem.inst, 1);
            }

            // VillagerSystem.inst.loadTickDelay = 1;
            Type villagerSystemType = typeof(VillagerSystem);
            loadTickDelayField = villagerSystemType.GetField("loadTickDelay", BindingFlags.Instance | BindingFlags.NonPublic);
            if (loadTickDelayField != null)
            {
                loadTickDelayField.SetValue(VillagerSystem.inst, 1);
            }

            Main.helper.Log($"Setting kingdom name to: {kingdomNames[Main.PlayerSteamID]}");
            TownNameUI.inst.SetTownName(kingdomNames[Main.PlayerSteamID]);

            return obj;
        }
    }
}
