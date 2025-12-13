using Assets.Code;
using KCM.Packets.Game.GameVillager;
using KCM.Packets.State;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KCM.StateManagement.Sync
{
    public static class SyncManager
    {
        private const int ResourceBroadcastIntervalMs = 2000;
        private const int MaxBuildingSnapshotBytes = 30000;
        private const int MaxVillagerTeleportsPerResync = 400;

        private static long lastResourceBroadcastMs;

        private static FieldInfo freeResourceAmountField;
        private static MethodInfo resourceAmountGetMethod;
        private static MethodInfo resourceAmountSetMethod;
        private static MethodInfo freeResourceManagerMaybeRefresh;
        private static MethodInfo fieldSystemMaybeRefresh;

        public static void ServerUpdate()
        {
            if (!KCServer.IsRunning)
                return;

            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((now - lastResourceBroadcastMs) < ResourceBroadcastIntervalMs)
                return;

            lastResourceBroadcastMs = now;

            try
            {
                ResourceSnapshotPacket snapshot = BuildResourceSnapshotPacket();
                if (snapshot == null)
                    return;

                snapshot.clientId = KCClient.client != null ? KCClient.client.Id : (ushort)0;

                // Exclude host/local client from receiving its own snapshot.
                ushort exceptId = KCClient.client != null ? KCClient.client.Id : (ushort)0;
                snapshot.SendToAll(exceptId);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error broadcasting resource snapshot");
                Main.helper.Log(ex.ToString());
            }
        }

        public static void SendResyncToClient(ushort toClient, string reason)
        {
            if (!KCServer.IsRunning)
                return;

            Main.helper.Log($"Resync requested by client {toClient} ({reason ?? ""})");

            try
            {
                ResourceSnapshotPacket snapshot = BuildResourceSnapshotPacket();
                if (snapshot != null)
                {
                    snapshot.clientId = KCClient.client != null ? KCClient.client.Id : (ushort)0;
                    snapshot.Send(toClient);
                }
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error sending resource resync");
                Main.helper.Log(ex.ToString());
            }

            try
            {
                SendBuildingSnapshotToClient(toClient);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error sending building resync");
                Main.helper.Log(ex.ToString());
            }

            try
            {
                SendVillagerTeleportSnapshotToClient(toClient);
            }
            catch (Exception ex)
            {
                Main.helper.Log("Error sending villager resync");
                Main.helper.Log(ex.ToString());
            }
        }

        private static ResourceSnapshotPacket BuildResourceSnapshotPacket()
        {
            List<int> types;
            List<int> amounts;
            if (!TryReadFreeResources(out types, out amounts))
                return null;

            return new ResourceSnapshotPacket
            {
                resourceTypes = types,
                amounts = amounts
            };
        }

        private static void SendBuildingSnapshotToClient(ushort toClient)
        {
            List<Building> buildings = new List<Building>();

            foreach (var p in Main.kCPlayers.Values)
            {
                if (p == null || p.inst == null)
                    continue;

                try
                {
                    var list = p.inst.Buildings;
                    for (int i = 0; i < list.Count; i++)
                    {
                        Building b = list.data[i];
                        if (b != null)
                            buildings.Add(b);
                    }
                }
                catch
                {
                }
            }

            if (buildings.Count == 0)
                return;

            int idx = 0;
            while (idx < buildings.Count)
            {
                byte[] payload = BuildBuildingSnapshotPayload(buildings, ref idx);
                if (payload == null || payload.Length == 0)
                    break;

                new BuildingSnapshotPacket { payload = payload }.Send(toClient);
            }
        }

        private static byte[] BuildBuildingSnapshotPayload(List<Building> buildings, ref int startIndex)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                long countPos = ms.Position;
                bw.Write(0); // placeholder for record count
                int written = 0;

                for (; startIndex < buildings.Count; startIndex++)
                {
                    Building b = buildings[startIndex];
                    if (b == null)
                        continue;

                    long before = ms.Position;
                    try
                    {
                        WriteBuildingRecord(bw, b);
                        written++;
                    }
                    catch
                    {
                        ms.Position = before;
                        ms.SetLength(before);
                    }

                    if (ms.Length >= MaxBuildingSnapshotBytes)
                    {
                        startIndex++;
                        break;
                    }
                }

                long endPos = ms.Position;
                ms.Position = countPos;
                bw.Write(written);
                ms.Position = endPos;

                return ms.ToArray();
            }
        }

        private static void WriteBuildingRecord(BinaryWriter bw, Building b)
        {
            bw.Write(b.TeamID());
            bw.Write(b.guid.ToByteArray());

            bw.Write(b.UniqueName ?? "");
            bw.Write(b.customName ?? "");

            Vector3 globalPosition = b.transform.position;
            Quaternion rotation = b.transform.childCount > 0 ? b.transform.GetChild(0).rotation : b.transform.rotation;
            Vector3 localPosition = b.transform.childCount > 0 ? b.transform.GetChild(0).localPosition : Vector3.zero;

            bw.Write(globalPosition.x);
            bw.Write(globalPosition.y);
            bw.Write(globalPosition.z);

            bw.Write(rotation.x);
            bw.Write(rotation.y);
            bw.Write(rotation.z);
            bw.Write(rotation.w);

            bw.Write(localPosition.x);
            bw.Write(localPosition.y);
            bw.Write(localPosition.z);

            bw.Write(b.IsBuilt());
            bw.Write(b.IsPlaced());
            bw.Write(b.Open);
            bw.Write(b.doBuildAnimation);
            bw.Write(b.constructionPaused);
            bw.Write(b.constructionProgress);

            float resourceProgress = 0f;
            try
            {
                var field = b.GetType().GetField("resourceProgress", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    resourceProgress = (float)field.GetValue(b);
            }
            catch { }
            bw.Write(resourceProgress);

            bw.Write(b.Life);
            bw.Write(b.ModifiedMaxLife);

            int yearBuilt = 0;
            try
            {
                var field = b.GetType().GetField("yearBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    yearBuilt = (int)field.GetValue(b);
            }
            catch { }
            bw.Write(yearBuilt);

            bw.Write(b.decayProtection);
        }

        public static void ApplyBuildingSnapshot(byte[] payload)
        {
            using (var ms = new MemoryStream(payload))
            using (var br = new BinaryReader(ms))
            {
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    int teamId = br.ReadInt32();
                    Guid guid = new Guid(br.ReadBytes(16));

                    string uniqueName = br.ReadString();
                    string customName = br.ReadString();

                    Vector3 globalPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    Quaternion rotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                    Vector3 localPosition = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

                    bool built = br.ReadBoolean();
                    bool placed = br.ReadBoolean();
                    bool open = br.ReadBoolean();
                    bool doBuildAnimation = br.ReadBoolean();
                    bool constructionPaused = br.ReadBoolean();
                    float constructionProgress = br.ReadSingle();
                    float resourceProgress = br.ReadSingle();
                    float life = br.ReadSingle();
                    float modifiedMaxLife = br.ReadSingle();
                    int yearBuilt = br.ReadInt32();
                    float decayProtection = br.ReadSingle();

                    ApplyBuildingRecord(teamId, guid, uniqueName, customName, globalPosition, rotation, localPosition, built, placed, open, doBuildAnimation, constructionPaused, constructionProgress, resourceProgress, life, modifiedMaxLife, yearBuilt, decayProtection);
                }
            }

            TryRefreshFieldSystem();
        }

        private static void ApplyBuildingRecord(int teamId, Guid guid, string uniqueName, string customName, Vector3 globalPosition, Quaternion rotation, Vector3 localPosition, bool built, bool placed, bool open, bool doBuildAnimation, bool constructionPaused, float constructionProgress, float resourceProgress, float life, float modifiedMaxLife, int yearBuilt, float decayProtection)
        {
            Player p = Main.GetPlayerByTeamID(teamId);
            if (p == null)
                return;

            Building building = null;
            try { building = p.GetBuilding(guid); } catch { }
            if (building == null)
                return;

            try
            {
                building.UniqueName = uniqueName;
                building.customName = customName;

                building.transform.position = globalPosition;
                if (building.transform.childCount > 0)
                {
                    building.transform.GetChild(0).rotation = rotation;
                    building.transform.GetChild(0).localPosition = localPosition;
                }
                else
                {
                    building.transform.rotation = rotation;
                }

                SetPrivateFieldValue(building, "built", built);
                SetPrivateFieldValue(building, "placed", placed);
                SetPrivateFieldValue(building, "resourceProgress", resourceProgress);
                SetPrivateFieldValue(building, "yearBuilt", yearBuilt);

                building.Open = open;
                building.doBuildAnimation = doBuildAnimation;
                building.constructionPaused = constructionPaused;
                building.constructionProgress = constructionProgress;
                building.Life = life;
                building.ModifiedMaxLife = modifiedMaxLife;
                building.decayProtection = decayProtection;
            }
            catch
            {
            }
        }

        private static void SetPrivateFieldValue(object obj, string fieldName, object value)
        {
            if (obj == null)
                return;

            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        private static void SendVillagerTeleportSnapshotToClient(ushort toClient)
        {
            try
            {
                int sent = 0;
                for (int i = 0; i < Villager.villagers.Count; i++)
                {
                    if (sent >= MaxVillagerTeleportsPerResync)
                        break;

                    Villager v = Villager.villagers.data[i];
                    if (v == null)
                        continue;

                    new VillagerTeleportTo
                    {
                        guid = v.guid,
                        pos = v.Pos
                    }.Send(toClient);

                    sent++;
                }
            }
            catch
            {
            }
        }

        public static void ApplyResourceSnapshot(List<int> resourceTypes, List<int> amounts)
        {
            if (resourceTypes == null || amounts == null)
                return;

            int count = Math.Min(resourceTypes.Count, amounts.Count);
            if (count == 0)
                return;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    FreeResourceType type = (FreeResourceType)resourceTypes[i];
                    int amount = amounts[i];
                    TryWriteFreeResource(type, amount);
                }
                catch
                {
                }
            }

            TryRefreshFreeResourceUI();
        }

        private static bool TryReadFreeResources(out List<int> types, out List<int> amounts)
        {
            types = new List<int>();
            amounts = new List<int>();

            if (FreeResourceManager.inst == null)
                return false;

            try
            {
                Array values = Enum.GetValues(typeof(FreeResourceType));
                foreach (var v in values)
                {
                    FreeResourceType t = (FreeResourceType)v;
                    int amount;
                    if (!TryReadFreeResource(t, out amount))
                        continue;

                    types.Add((int)t);
                    amounts.Add(amount);
                }
            }
            catch
            {
                return false;
            }

            return types.Count > 0;
        }

        private static bool EnsureResourceReflection()
        {
            if (resourceAmountGetMethod != null && resourceAmountSetMethod != null && freeResourceAmountField != null)
                return true;

            try
            {
                Type raType = typeof(ResourceAmount);
                resourceAmountGetMethod = raType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(FreeResourceType) }, null);
                resourceAmountSetMethod = raType.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(FreeResourceType), typeof(int) }, null);

                var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                freeResourceAmountField = typeof(FreeResourceManager).GetFields(bindingFlags).FirstOrDefault(f => f.FieldType == raType);
                if (freeResourceAmountField == null)
                {
                    var prop = typeof(FreeResourceManager).GetProperties(bindingFlags).FirstOrDefault(p => p.PropertyType == raType && p.GetGetMethod(true) != null);
                    if (prop != null)
                    {
                        // Fallback: treat property getter as "field" by caching getter only.
                        // We won't be able to set back reliably in this case.
                    }
                }

                freeResourceManagerMaybeRefresh = typeof(FreeResourceManager).GetMethods(bindingFlags)
                    .FirstOrDefault(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void) && (m.Name.IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0));
            }
            catch
            {
                return false;
            }

            return freeResourceAmountField != null && resourceAmountGetMethod != null && resourceAmountSetMethod != null;
        }

        private static bool TryReadFreeResource(FreeResourceType type, out int amount)
        {
            amount = 0;

            if (!EnsureResourceReflection())
                return false;

            try
            {
                object ra = freeResourceAmountField.GetValue(FreeResourceManager.inst);
                if (ra == null)
                    return false;

                object result = resourceAmountGetMethod.Invoke(ra, new object[] { type });
                if (result is int)
                {
                    amount = (int)result;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryWriteFreeResource(FreeResourceType type, int amount)
        {
            if (!EnsureResourceReflection())
                return false;

            try
            {
                object ra = freeResourceAmountField.GetValue(FreeResourceManager.inst);
                if (ra == null)
                    return false;

                resourceAmountSetMethod.Invoke(ra, new object[] { type, amount });
                if (typeof(ResourceAmount).IsValueType)
                    freeResourceAmountField.SetValue(FreeResourceManager.inst, ra);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TryRefreshFreeResourceUI()
        {
            try
            {
                if (!EnsureResourceReflection())
                    return;

                if (freeResourceManagerMaybeRefresh != null && FreeResourceManager.inst != null)
                    freeResourceManagerMaybeRefresh.Invoke(FreeResourceManager.inst, null);
            }
            catch
            {
            }
        }

        private static void TryRefreshFieldSystem()
        {
            try
            {
                if (Player.inst == null || Player.inst.fieldSystem == null)
                    return;

                if (fieldSystemMaybeRefresh == null)
                {
                    var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    fieldSystemMaybeRefresh = Player.inst.fieldSystem.GetType()
                        .GetMethods(bindingFlags)
                        .FirstOrDefault(m =>
                            m.ReturnType == typeof(void) &&
                            m.GetParameters().Length == 0 &&
                            (m.Name.IndexOf("Rebuild", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             m.Name.IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0) &&
                            m.Name.IndexOf("Reset", StringComparison.OrdinalIgnoreCase) < 0 &&
                            m.Name.IndexOf("Clear", StringComparison.OrdinalIgnoreCase) < 0);
                }

                if (fieldSystemMaybeRefresh != null)
                    fieldSystemMaybeRefresh.Invoke(Player.inst.fieldSystem, null);
            }
            catch
            {
            }
        }
    }
}
