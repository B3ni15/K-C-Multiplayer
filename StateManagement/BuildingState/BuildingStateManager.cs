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

        public static void BuildingStateChanged(object sender, StateUpdateEventArgs args)
        {

        }

        public static void SendBuildingUpdate(object sender, StateUpdateEventArgs args)
        {
            try
            {
                Observer observer = (Observer)sender;

                Building building = (Building)observer.state;

                //Main.helper.Log("Should send building network update for: " + building.UniqueName);

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
