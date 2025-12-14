using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.Game.GameWorld
{
    public class WorldPlace : Packet
    {
        public override ushort packetId => (int)Enums.Packets.WorldPlace;

        public string customName { get; set; }
        public Guid guid { get; set; }
        public string uniqueName { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 globalPosition { get; set; }
        public Vector3 localPosition { get; set; }
        public bool built { get; set; }
        public bool placed { get; set; }
        public bool open { get; set; }
        public bool doBuildAnimation { get; set; }
        public bool constructionPaused { get; set; }
        public float constructionProgress { get; set; }
        public float life { get; set; }
        public float ModifiedMaxLife { get; set; }
        public int yearBuilt { get; set; }
        public float decayProtection { get; set; }
        public bool seenByPlayer { get; set; }

        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) return; //prevent double placing on same client

            PlaceBuilding();
        }

        public override void HandlePacketServer()
        {
            //PlaceBuilding();

            //SendToAll(clientId);
        }

        public void PlaceBuilding()
        {
            Main.LogSync("========== BUILDING PLACEMENT START ==========");
            Main.LogSync($"Building: {uniqueName} from player: {player?.name} (id={player?.id})");
            Main.LogSync($"  guid={guid}");
            Main.LogSync($"  globalPosition={globalPosition}");
            Main.LogSync($"  localPosition={localPosition}");
            Main.LogSync($"  rotation={rotation} (euler={rotation.eulerAngles})");
            Main.LogSync($"  built={built}, placed={placed}, open={open}");
            Main.LogSync($"  constructionProgress={constructionProgress}, constructionPaused={constructionPaused}");
            Main.LogSync($"  life={life}, ModifiedMaxLife={ModifiedMaxLife}");
            Main.LogSync($"  yearBuilt={yearBuilt}, decayProtection={decayProtection}");

            // Check for duplicate building by guid to prevent double placement from network retries
            var existingBuilding = player.inst.Buildings.data.FirstOrDefault(b => b != null && b.guid == guid);
            if (existingBuilding != null)
            {
                Main.helper.Log($"Building with guid {guid} already exists for player {player.name}, skipping duplicate placement");
                return;
            }

            //var originalPlayer = Player.inst;
            //Player.inst = player.inst;

            Building.BuildingSaveData structureData = new Building.BuildingSaveData()
            {
                uniqueName = uniqueName,
                customName = customName,
                guid = guid,
                rotation = rotation,
                globalPosition = globalPosition,
                localPosition = localPosition,
                built = built,
                placed = placed,
                open = open,
                doBuildAnimation = doBuildAnimation,
                constructionPaused = constructionPaused,
                constructionProgress = constructionProgress,
                life = life,
                ModifiedMaxLife = ModifiedMaxLife,
                //CollectForBuild = CollectForBuild,
                yearBuilt = yearBuilt,
                decayProtection = decayProtection,
                seenByPlayer = seenByPlayer
            };


            //Player originalInst = Player.inst;
            //Player.inst = player.inst;

            Building Building = GameState.inst.GetPlaceableByUniqueName(structureData.uniqueName);
            bool flag = Building;
            if (flag)
            {
                Building building = UnityEngine.Object.Instantiate<Building>(Building);
                building.transform.position = structureData.globalPosition;
                Main.helper.Log("Building init");
                building.Init();
                building.transform.SetParent(player.inst.buildingContainer.transform, true);
                Main.helper.Log("Building unpack");
                structureData.Unpack(building);

                Main.helper.Log(player.inst.ToString());
                Main.helper.Log((player.inst.PlayerLandmassOwner == null).ToString());
                Main.helper.Log(building.LandMass().ToString());
                Main.helper.Log("Player add Building unpacked");
                player.inst.AddBuilding(building);
                Main.ApplyPendingBuildingState(building);

                try
                {

                    player.inst.PlayerLandmassOwner.TakeOwnership(building.LandMass());
                    bool flag2 = building.GetComponent<Keep>() != null && building.TeamID() == player.inst.PlayerLandmassOwner.teamId;
                    Main.helper.Log("Set keep " + flag2);
                    if (flag2)
                    {
                        player.inst.keep = building.GetComponent<Keep>();
                    }
                }
                catch (Exception e)
                {
                    Main.helper.Log(e.Message);
                }

                Main.helper.Log("Place from load");
                Cell cell = World.inst.PlaceFromLoad(building);
                Main.helper.Log("unpack stage 2");
                structureData.UnpackStage2(building);

                // Update materials/textures for correct display
                building.UpdateMaterialSelection();

                // Update road rotation for proper visuals
                Road roadComp = building.GetComponent<Road>();
                if (roadComp != null)
                {
                    roadComp.UpdateRotation();
                }

                // Update aqueduct rotation
                Aqueduct aqueductComp = building.GetComponent<Aqueduct>();
                if (aqueductComp != null)
                {
                    aqueductComp.UpdateRotation();
                }

                building.SetVisibleForFog(false);

                Main.helper.Log("Landmass owner take ownership");

                Main.helper.Log($"{player.id} (team {player.inst.PlayerLandmassOwner.teamId}) banner: {player.inst.PlayerLandmassOwner.bannerIdx} Placed building {building.name} at {building.transform.position}");


                //Player.inst = originalInst; // Reset player back to normal // Might not be needed anymore with player ref patches?


                Main.helper.Log($"Host player Landmass Names Count: {Player.inst.LandMassNames.Count}, Contents: {string.Join(", ", Player.inst.LandMassNames)}");
                Main.helper.Log($"Client player ({player.name}) Landmass Names Count: {player.inst.LandMassNames.Count}, Contents: {string.Join(", ", player.inst.LandMassNames)}");

                player.inst.LandMassNames[building.LandMass()] = player.kingdomName;
                Player.inst.LandMassNames[building.LandMass()] = player.kingdomName;

                // Log final building state after placement
                Main.LogSync("---------- BUILDING PLACED FINAL STATE ----------");
                Main.LogSync($"  Final position: {building.transform.position}");
                if (building.transform.childCount > 0)
                {
                    Main.LogSync($"  Child[0] rotation: {building.transform.GetChild(0).rotation} (euler={building.transform.GetChild(0).rotation.eulerAngles})");
                    Main.LogSync($"  Child[0] localPosition: {building.transform.GetChild(0).localPosition}");
                }
                Main.LogSync($"  IsBuilt={building.IsBuilt()}, IsPlaced={building.IsPlaced()}");
                Main.LogSync($"  TeamID={building.TeamID()}, LandMass={building.LandMass()}");
                Main.LogSync("========== BUILDING PLACEMENT END ==========");
            }
            else
            {
                Main.LogSync($"FAILED to place building: {structureData.uniqueName} - GetPlaceableByUniqueName returned null");
            }
        }

    }
}
