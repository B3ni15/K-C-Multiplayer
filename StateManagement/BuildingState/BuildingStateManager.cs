using KCM.Packets;
using KCM.Packets.State;
using KCM.StateManagement.Observers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                Observer observer = sender as Observer;
                if (observer == null)
                    return;

                Building building = observer.state as Building;
                if (building == null)
                    return;

                //Main.helper.Log("Should send building network update for: " + building.UniqueName);

                var t = building.transform;
                if (t == null)
                    return;

                Quaternion rotation = t.rotation;
                Vector3 globalPosition = t.position;
                Vector3 localPosition = t.localPosition;

                if (t.childCount > 0)
                {
                    try
                    {
                        var child = t.GetChild(0);
                        if (child != null)
                        {
                            rotation = child.rotation;
                            localPosition = child.localPosition;
                        }
                    }
                    catch
                    {
                    }
                }

                float resourceProgress = 0f;
                try
                {
                    var field = building.GetType().GetField("resourceProgress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        object value = field.GetValue(building);
                        if (value is float)
                            resourceProgress = (float)value;
                        else if (value != null)
                            resourceProgress = Convert.ToSingle(value);
                    }
                }
                catch
                {
                }

                new BuildingStatePacket()
                {
                    customName = building.customName,
                    guid = building.guid,
                    uniqueName = building.UniqueName,
                    rotation = rotation,
                    globalPosition = globalPosition,
                    localPosition = localPosition,
                    built = building.IsBuilt(),
                    placed = building.IsPlaced(),
                    open = building.Open,
                    doBuildAnimation = building.doBuildAnimation,
                    constructionPaused = building.constructionPaused,
                    constructionProgress = building.constructionProgress,
                    resourceProgress = resourceProgress,
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
