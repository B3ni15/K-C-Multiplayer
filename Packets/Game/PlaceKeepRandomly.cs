using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;

namespace KCM.Packets.Game
{
    public class PlaceKeepRandomly : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.PlaceKeepRandomly;

        public int landmassIdx { get; set; }

        public override void HandlePacketClient()
        {
            try
            {
                Building keep = UnityEngine.Object.Instantiate<Building>(GameState.inst.GetPlaceableByUniqueName(World.keepName));

                keep.Init();


                Cell[] cells = World.inst.GetCellsData().Where(x => x.landMassIdx == landmassIdx).ToArray();
                Cell keepCell = null;


                foreach (Cell cell in cells)
                {
                    Cell nearbyStoneCell = FindNearbyStoneCell(cells, cell.x, cell.z, landmassIdx, 15); // Place keep within 15 tiles of stone
                    Cell nearbyWaterCell = FindNearbyWaterCell(cells, cell.x, cell.z, landmassIdx, 6); // Do not place keep within 6 tiles of water


                    Cell clearCell = FindClearCell(cells, cell.x, cell.z, landmassIdx, 4); // cells in 4 by 4 radius are clear?

                    if (clearCell != null & nearbyStoneCell != null && nearbyWaterCell == null && cell.Type == ResourceType.None)
                    {
                        Console.WriteLine($"Nearby stone cell found at ({nearbyStoneCell.x}, {nearbyStoneCell.z})");
                        keepCell = cell;

                        break;
                    }
                    else
                        continue;

                }

                keep.transform.position = keepCell.Position;

                keep.SendMessage("OnPlayerPlacement", SendMessageOptions.DontRequireReceiver);


                Player.inst.PlayerLandmassOwner.TakeOwnership(keep.LandMass());
                Player.inst.keep = keep.GetComponent<Keep>();
                Player.inst.RefreshVisibility(true);
                RandomPlacement(keep);

            } catch (Exception e)
            {
                Main.helper.Log($"Error placing keep randomly: {e.Message}");
            }
        }

        private void RandomPlacement(Building keep) // This is a hack so I can detect when its being called by this packet 
        {
            World.inst.Place(keep);

            Cam.inst.SetTrackingPos(keep.GetPosition());
        }

        private static Cell FindNearbyStoneCell(Cell[] cells, int x, int z, int landmassIdx, int radius)
        {
            return cells.FirstOrDefault(cell => IsResourceInRadius(cell, x, z, radius, ResourceType.Stone));
        }

        private static Cell FindNearbyWaterCell(Cell[] cells, int x, int z, int landmassIdx, int radius)
        {
            return cells.FirstOrDefault(cell => IsResourceInRadius(cell, x, z, radius, ResourceType.Water));
        }
        private static Cell FindClearCell(Cell[] cells, int x, int z, int landmassIdx, int radius)
        {
            return cells.FirstOrDefault(cell => IsResourceInRadius(cell, x, z, radius, ResourceType.None));
        }

        private static bool IsResourceInRadius(Cell cell, int x, int z, int radius, ResourceType desiredResource)
        {
            bool isWithinRadius = Math.Sqrt((cell.x - x) * (cell.x - x) + (cell.z - z) * (cell.z - z)) <= radius;
            bool isNotCentralCell = cell.x != x || cell.z != z;
            bool isStoneType = cell.Type == desiredResource;

            bool isWater = desiredResource == ResourceType.Water ? false : cell.deepWater || cell.Type == ResourceType.Water;

            return isWithinRadius && isNotCentralCell && isStoneType && !isWater;
        }

        public override void HandlePacketServer()
        {
        }
    }
}
