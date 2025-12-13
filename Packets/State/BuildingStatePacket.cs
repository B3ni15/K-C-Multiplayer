using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace KCM.Packets.State
{
    public class BuildingStatePacket : Packet
    {
        public override ushort packetId => (ushort)Enums.Packets.BuildingStatePacket;
        public override Riptide.MessageSendMode sendMode => Riptide.MessageSendMode.Unreliable;

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
        public float resourceProgress { get; set; }
        public float life { get; set; }
        public float ModifiedMaxLife { get; set; }
        public int yearBuilt { get; set; }
        public float decayProtection { get; set; }
        public bool seenByPlayer { get; set; }


        public override void HandlePacketClient()
        {
            if (clientId == KCClient.client.Id) return; //prevent double placing on same client

            //Main.helper.Log("Received building state packet for: " + uniqueName + " from " + Main.kCPlayers[Main.GetPlayerByClientID(clientId).steamId].name + $"({clientId})");


            Building building = player.inst.GetBuilding(guid);

            if (building == null)
            {
                Main.helper.Log("Building not found.");
                return;
            }

            try
            {
                //PrintProperties();

                building.UniqueName = uniqueName;
                building.customName = customName;


                building.transform.position = this.globalPosition;
                building.transform.GetChild(0).rotation = this.rotation;
                building.transform.GetChild(0).localPosition = this.localPosition;

                SetPrivateFieldValue(building, "built", built);
                SetPrivateFieldValue(building, "placed", placed);
                SetPrivateFieldValue(building, "resourceProgress", resourceProgress);


                building.Open = open;
                building.doBuildAnimation = doBuildAnimation;
                building.constructionPaused = constructionPaused;
                building.constructionProgress = constructionProgress;
                building.Life = life;
                building.ModifiedMaxLife = ModifiedMaxLife;


                //building.yearBuilt = yearBuilt;
                SetPrivateFieldValue(building, "yearBuilt", yearBuilt);

                building.decayProtection = decayProtection;
                //building.seenByPlayer = seenByPlayer;
            }
            catch (Exception e)
            {
                Main.helper.Log("Error setting building state");
                Main.helper.Log(e.Message);
                Main.helper.Log(e.StackTrace);
            }
        }

        public override void HandlePacketServer()
        {
            //throw new NotImplementedException();
        }

        private void SetPrivateFieldValue(object obj, string fieldName, object value)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        public void PrintProperties()
        {
            Type type = typeof(BuildingStatePacket);

            foreach (PropertyInfo property in type.GetProperties())
            {
                object value = property.GetValue(this);
                string propertyName = property.Name;

                Main.helper.Log($"{propertyName}: {value}");
            }
        }

    }
}
