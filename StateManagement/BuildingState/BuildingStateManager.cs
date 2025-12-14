using KCM.Packets;
using KCM.Packets.State;
using KCM.StateManagement.Observers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static KCM.StateManagement.Observers.Observer;

namespace KCM.StateManagement.BuildingState
{
    public class BuildingStateManager
    {
        private static readonly Dictionary<Guid, float> lastUpdateTime = new Dictionary<Guid, float>();
        private const float UpdateInterval = 0.1f; // 10 times per second

        public static void BuildingStateChanged(object sender, StateUpdateEventArgs args)
        {

        }

        public static void SendBuildingUpdate(object sender, StateUpdateEventArgs args)
        {
            try
            {
                Observer observer = (Observer)sender;
                Building building = (Building)observer.state;
                Guid guid = building.guid;

                if (lastUpdateTime.ContainsKey(guid) && Time.time < lastUpdateTime[guid] + UpdateInterval)
                {
                    return; // Not time to update yet
                }

                if (!lastUpdateTime.ContainsKey(guid))
                    lastUpdateTime.Add(guid, Time.time);
                else
                    lastUpdateTime[guid] = Time.time;

                new BuildingStatePacket()
                {
                    customName = building.customName,
                    guid = building.guid,
                    uniqueName = building.UniqueName,
                    rotation = building.transform.GetChild(0).rotation,
                    globalPosition = building.transform.position,
                    localPosition = building.transform.GetChild(0).localPosition,
                    built = building.IsBuilt(),
                    placed = building.IsPlaced(),
                    open = building.Open,
                    doBuildAnimation = building.doBuildAnimation,
                    constructionPaused = building.constructionPaused,
                    constructionProgress = building.constructionProgress,
                    resourceProgress = (float)building.GetType().GetField("resourceProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(building),
                    life = building.Life,
                    ModifiedMaxLife = building.ModifiedMaxLife,
                    yearBuilt = building.YearBuilt,
                    decayProtection = building.decayProtection,
                    seenByPlayer = building.seenByPlayer
                }.Send();
            } catch (Exception e)
            {
                Main.helper.Log("ERror sending building state packet");
                Main.helper.Log(e.Message);
                Main.helper.Log(e.StackTrace);
            }
        }
    }
}