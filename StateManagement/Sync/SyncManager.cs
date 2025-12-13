using Assets.Code;
using KCM.Packets.Game.GameVillager;
using KCM.Packets.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace KCM.StateManagement.Sync
{
    public static class SyncManager
    {
        private const int ResourceBroadcastIntervalMs = 2000;
        private const int MaxBuildingSnapshotBytes = 30000;
        private const int MaxVillagerTeleportsPerResync = 400;
        private const int VillagerValidationIntervalMs = 10000; // 10 seconds
        private const int VillagerSnapshotIntervalMs = 1000;

        private static long lastResourceBroadcastMs;
        private static long lastVillagerValidationMs;
        private static long lastVillagerSnapshotMs;

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
            
            // Resource broadcast
            if ((now - lastResourceBroadcastMs) >= ResourceBroadcastIntervalMs)
            {
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
            
            // Villager state validation
            if ((now - lastVillagerValidationMs) >= VillagerValidationIntervalMs)
            {
                lastVillagerValidationMs = now;
                ValidateAndCorrectVillagerStates();
            }

            if ((now - lastVillagerSnapshotMs) >= VillagerSnapshotIntervalMs)
            {
                lastVillagerSnapshotMs = now;
                try
                {
                    BroadcastVillagerSnapshot();
                }
                catch (Exception ex)
                {
                    Main.helper.Log("Error broadcasting villager snapshot");
                    Main.helper.Log(ex.ToString());
                }
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
            byte[] buffer = new byte[MaxBuildingSnapshotBytes];
            int offset = 0;
            int countOffset = offset;
            if (!TryWriteInt32(buffer, ref offset, 0))
                return new byte[0];

            int written = 0;
            for (; startIndex < buildings.Count; startIndex++)
            {
                Building b = buildings[startIndex];
                if (b == null)
                    continue;

                int before = offset;
                if (!TryWriteBuildingRecord(buffer, ref offset, b))
                {
                    offset = before;
                    startIndex++;
                    break;
                }

                written++;

                if (offset >= MaxBuildingSnapshotBytes - 256)
                {
                    startIndex++;
                    break;
                }
            }

            WriteInt32At(buffer, countOffset, written);
            byte[] result = new byte[offset];
            Buffer.BlockCopy(buffer, 0, result, 0, offset);
            return result;
        }

        private static bool TryWriteBuildingRecord(byte[] buffer, ref int offset, Building b)
        {
            if (!TryWriteInt32(buffer, ref offset, b.TeamID()))
                return false;
            if (!TryWriteGuidBytes(buffer, ref offset, b.guid))
                return false;

            if (!TryWriteString(buffer, ref offset, b.UniqueName ?? ""))
                return false;
            if (!TryWriteString(buffer, ref offset, b.customName ?? ""))
                return false;

            Vector3 globalPosition = b.transform.position;
            Quaternion rotation = b.transform.childCount > 0 ? b.transform.GetChild(0).rotation : b.transform.rotation;
            Vector3 localPosition = b.transform.childCount > 0 ? b.transform.GetChild(0).localPosition : Vector3.zero;

            if (!TryWriteSingle(buffer, ref offset, globalPosition.x) ||
                !TryWriteSingle(buffer, ref offset, globalPosition.y) ||
                !TryWriteSingle(buffer, ref offset, globalPosition.z))
                return false;

            if (!TryWriteSingle(buffer, ref offset, rotation.x) ||
                !TryWriteSingle(buffer, ref offset, rotation.y) ||
                !TryWriteSingle(buffer, ref offset, rotation.z) ||
                !TryWriteSingle(buffer, ref offset, rotation.w))
                return false;

            if (!TryWriteSingle(buffer, ref offset, localPosition.x) ||
                !TryWriteSingle(buffer, ref offset, localPosition.y) ||
                !TryWriteSingle(buffer, ref offset, localPosition.z))
                return false;

            if (!TryWriteBool(buffer, ref offset, b.IsBuilt()) ||
                !TryWriteBool(buffer, ref offset, b.IsPlaced()) ||
                !TryWriteBool(buffer, ref offset, b.Open) ||
                !TryWriteBool(buffer, ref offset, b.doBuildAnimation) ||
                !TryWriteBool(buffer, ref offset, b.constructionPaused))
                return false;

            if (!TryWriteSingle(buffer, ref offset, b.constructionProgress))
                return false;

            float resourceProgress = 0f;
            try
            {
                var field = b.GetType().GetField("resourceProgress", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    resourceProgress = (float)field.GetValue(b);
            }
            catch { }
            if (!TryWriteSingle(buffer, ref offset, resourceProgress))
                return false;

            if (!TryWriteSingle(buffer, ref offset, b.Life) ||
                !TryWriteSingle(buffer, ref offset, b.ModifiedMaxLife))
                return false;

            int yearBuilt = 0;
            try
            {
                var field = b.GetType().GetField("yearBuilt", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                    yearBuilt = (int)field.GetValue(b);
            }
            catch { }
            if (!TryWriteInt32(buffer, ref offset, yearBuilt))
                return false;

            if (!TryWriteSingle(buffer, ref offset, b.decayProtection))
                return false;

            return true;
        }

        public static void ApplyBuildingSnapshot(byte[] payload)
        {
            if (payload == null || payload.Length < 4)
                return;

            int offset = 0;
            int count;
            if (!TryReadInt32(payload, ref offset, out count))
                return;

            for (int i = 0; i < count; i++)
            {
                int teamId;
                Guid guid;
                string uniqueName;
                string customName;
                Vector3 globalPosition;
                Quaternion rotation;
                Vector3 localPosition;
                bool built;
                bool placed;
                bool open;
                bool doBuildAnimation;
                bool constructionPaused;
                float constructionProgress;
                float resourceProgress;
                float life;
                float modifiedMaxLife;
                int yearBuilt;
                float decayProtection;

                if (!TryReadInt32(payload, ref offset, out teamId))
                    break;
                if (!TryReadGuid(payload, ref offset, out guid))
                    break;
                if (!TryReadString(payload, ref offset, out uniqueName))
                    break;
                if (!TryReadString(payload, ref offset, out customName))
                    break;

                float gx, gy, gz;
                float rx, ry, rz, rw;
                float lx, ly, lz;
                if (!TryReadSingle(payload, ref offset, out gx) ||
                    !TryReadSingle(payload, ref offset, out gy) ||
                    !TryReadSingle(payload, ref offset, out gz))
                    break;
                globalPosition = new Vector3(gx, gy, gz);

                if (!TryReadSingle(payload, ref offset, out rx) ||
                    !TryReadSingle(payload, ref offset, out ry) ||
                    !TryReadSingle(payload, ref offset, out rz) ||
                    !TryReadSingle(payload, ref offset, out rw))
                    break;
                rotation = new Quaternion(rx, ry, rz, rw);

                if (!TryReadSingle(payload, ref offset, out lx) ||
                    !TryReadSingle(payload, ref offset, out ly) ||
                    !TryReadSingle(payload, ref offset, out lz))
                    break;
                localPosition = new Vector3(lx, ly, lz);

                if (!TryReadBool(payload, ref offset, out built) ||
                    !TryReadBool(payload, ref offset, out placed) ||
                    !TryReadBool(payload, ref offset, out open) ||
                    !TryReadBool(payload, ref offset, out doBuildAnimation) ||
                    !TryReadBool(payload, ref offset, out constructionPaused))
                    break;

                if (!TryReadSingle(payload, ref offset, out constructionProgress) ||
                    !TryReadSingle(payload, ref offset, out resourceProgress) ||
                    !TryReadSingle(payload, ref offset, out life) ||
                    !TryReadSingle(payload, ref offset, out modifiedMaxLife) ||
                    !TryReadInt32(payload, ref offset, out yearBuilt) ||
                    !TryReadSingle(payload, ref offset, out decayProtection))
                    break;

                ApplyBuildingRecord(teamId, guid, uniqueName, customName, globalPosition, rotation, localPosition, built, placed, open, doBuildAnimation, constructionPaused, constructionProgress, resourceProgress, life, modifiedMaxLife, yearBuilt, decayProtection);
            }

            TryRefreshFieldSystem();
        }

        private static bool EnsureCapacity(byte[] buffer, int offset, int bytesToWrite)
        {
            return buffer != null && offset >= 0 && (offset + bytesToWrite) <= buffer.Length;
        }

        private static bool TryWriteInt32(byte[] buffer, ref int offset, int value)
        {
            if (!EnsureCapacity(buffer, offset, 4))
                return false;
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
            offset += 4;
            return true;
        }

        private static void WriteInt32At(byte[] buffer, int offset, int value)
        {
            if (!EnsureCapacity(buffer, offset, 4))
                return;
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
        }

        private static bool TryWriteSingle(byte[] buffer, ref int offset, float value)
        {
            if (!EnsureCapacity(buffer, offset, 4))
                return false;
            byte[] bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, 4);
            offset += 4;
            return true;
        }

        private static bool TryWriteBool(byte[] buffer, ref int offset, bool value)
        {
            if (!EnsureCapacity(buffer, offset, 1))
                return false;
            buffer[offset++] = (byte)(value ? 1 : 0);
            return true;
        }

        private static bool TryWriteGuidBytes(byte[] buffer, ref int offset, Guid guid)
        {
            if (!EnsureCapacity(buffer, offset, 16))
                return false;
            byte[] bytes = guid.ToByteArray();
            Buffer.BlockCopy(bytes, 0, buffer, offset, 16);
            offset += 16;
            return true;
        }

        private static bool TryWriteString(byte[] buffer, ref int offset, string value)
        {
            if (value == null)
                value = "";

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (!TryWriteInt32(buffer, ref offset, bytes.Length))
                return false;
            if (!EnsureCapacity(buffer, offset, bytes.Length))
                return false;
            Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
            offset += bytes.Length;
            return true;
        }

        private static bool TryReadInt32(byte[] buffer, ref int offset, out int value)
        {
            value = 0;
            if (!EnsureCapacity(buffer, offset, 4))
                return false;
            value = BitConverter.ToInt32(buffer, offset);
            offset += 4;
            return true;
        }

        private static bool TryReadSingle(byte[] buffer, ref int offset, out float value)
        {
            value = 0f;
            if (!EnsureCapacity(buffer, offset, 4))
                return false;
            value = BitConverter.ToSingle(buffer, offset);
            offset += 4;
            return true;
        }

        private static bool TryReadBool(byte[] buffer, ref int offset, out bool value)
        {
            value = false;
            if (!EnsureCapacity(buffer, offset, 1))
                return false;
            value = buffer[offset++] != 0;
            return true;
        }

        private static bool TryReadGuid(byte[] buffer, ref int offset, out Guid value)
        {
            value = Guid.Empty;
            if (!EnsureCapacity(buffer, offset, 16))
                return false;
            byte[] bytes = new byte[16];
            Buffer.BlockCopy(buffer, offset, bytes, 0, 16);
            offset += 16;
            value = new Guid(bytes);
            return true;
        }

        private static bool TryReadString(byte[] buffer, ref int offset, out string value)
        {
            value = "";
            int len;
            if (!TryReadInt32(buffer, ref offset, out len))
                return false;
            if (len < 0 || !EnsureCapacity(buffer, offset, len))
                return false;
            value = len == 0 ? "" : Encoding.UTF8.GetString(buffer, offset, len);
            offset += len;
            return true;
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

        private static void BroadcastVillagerSnapshot()
        {
            // TEMPORARILY DISABLED: VillagerSnapshot causes packet overflow errors
            // TODO: Fix villager synchronization properly
            return;

            // if (!KCServer.IsRunning)
            //     return;

            // if (KCServer.server.ClientCount == 0)
            //     return;

            // if (Villager.villagers == null || Villager.villagers.Count == 0)
            //     return;

            List<Guid> guids = new List<Guid>();
            List<Vector3> positions = new List<Vector3>();
            const int maxVillagersPerSnapshot = 50;

            for (int i = 0; i < Villager.villagers.Count && guids.Count < maxVillagersPerSnapshot; i++)
            {
                Villager villager = Villager.villagers.data[i];
                if (villager == null)
                    continue;

                guids.Add(villager.guid);
                positions.Add(villager.Pos);
            }

            if (guids.Count == 0)
                return;

            VillagerSnapshotPacket snapshot = new VillagerSnapshotPacket
            {
                guids = guids,
                positions = positions
            };

            ushort exceptId = KCClient.client != null ? KCClient.client.Id : (ushort)0;
            snapshot.SendToAll(exceptId);
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

        private static void ValidateAndCorrectVillagerStates()
        {
            try
            {
                int stuckVillagers = 0;
                int correctedVillagers = 0;
                
                for (int i = 0; i < Villager.villagers.Count; i++)
                {
                    Villager v = Villager.villagers.data[i];
                    if (v == null)
                        continue;
                        
                    try
                    {
                        bool needsCorrection = false;
                        
                        // Check if villager position is invalid
                        if (float.IsNaN(v.Pos.x) || float.IsNaN(v.Pos.y) || float.IsNaN(v.Pos.z))
                        {
                            needsCorrection = true;
                            stuckVillagers++;
                        }
                        
                        if (needsCorrection)
                        {
                            // Correct villager state
                            try
                            {
                                // Ensure valid position
                                if (float.IsNaN(v.Pos.x) || float.IsNaN(v.Pos.y) || float.IsNaN(v.Pos.z))
                                {
                                    // Teleport to a safe position
                                    Vector3 safePos = new Vector3(World.inst.GridWidth / 2, 0, World.inst.GridHeight / 2);
                                    v.TeleportTo(safePos);
                                }
                                
                                correctedVillagers++;
                            }
                            catch (Exception e)
                            {
                                Main.helper.Log($"Error correcting villager {i}: {e.Message}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log($"Error validating villager {i}: {e.Message}");
                    }
                }
                
                if (stuckVillagers > 0)
                {
                    Main.helper.Log($"Villager validation: Found {stuckVillagers} stuck villagers, corrected {correctedVillagers}");
                }
                
                // Force villager system refresh if we found issues
                if (stuckVillagers > 0 && VillagerSystem.inst != null)
                {
                    try
                    {
                        var villagerSystemType = typeof(VillagerSystem);
                        var refreshMethods = villagerSystemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            .Where(m => m.Name.Contains("Refresh") || m.Name.Contains("Update") || m.Name.Contains("Restart"));
                            
                        foreach (var method in refreshMethods)
                        {
                            if (method.GetParameters().Length == 0)
                            {
                                try
                                {
                                    method.Invoke(VillagerSystem.inst, null);
                                    Main.helper.Log($"Called VillagerSystem.{method.Name} for validation");
                                }
                                catch { }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Main.helper.Log($"Error refreshing villager system: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Main.helper.Log("Error in villager state validation: " + e.Message);
            }
        }
    }
}
