using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace KCM
{
    internal static class ResourceStorageHelper
    {
        private static Type resourceStorageType;
        private static MethodInfo getComponentsMethod;
        private static MethodInfo isPrivateMethod;
        private static MethodInfo addResourceStorageMethod;

        private static readonly string[] resourceStorageTypeNames = new[]
        {
            "Assets.Interface.IResourceStorage, Assembly-CSharp",
            "Assets.Interface.IResourceStorage, Assembly-CSharp-firstpass",
            "Assets.Interface.IResourceStorage, Assembly-CSharp-Editor",
        };

        private static void EnsureInitialized()
        {
            if (resourceStorageType != null)
                return;

            foreach (var typeName in resourceStorageTypeNames)
            {
                resourceStorageType = Type.GetType(typeName);
                if (resourceStorageType != null)
                    break;
            }

            if (resourceStorageType == null)
                return;

            getComponentsMethod = typeof(Component).GetMethod("GetComponents", new[] { typeof(Type) });
            isPrivateMethod = resourceStorageType.GetMethod("IsPrivate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            addResourceStorageMethod = typeof(FreeResourceManager).GetMethod("AddResourceStorage", new[] { resourceStorageType });
        }

        public static Component[] GetStorages(Component owner)
        {
            EnsureInitialized();
            if (resourceStorageType == null || getComponentsMethod == null)
                return Array.Empty<Component>();

            return (Component[])getComponentsMethod.Invoke(owner, new object[] { resourceStorageType });
        }

        public static bool IsPrivate(Component storage)
        {
            EnsureInitialized();
            if (isPrivateMethod == null)
                return true;

            return (bool)isPrivateMethod.Invoke(storage, null);
        }

        public static void Register(Component storage)
        {
            EnsureInitialized();
            if (addResourceStorageMethod == null || FreeResourceManager.inst == null)
                return;

            addResourceStorageMethod.Invoke(FreeResourceManager.inst, new object[] { storage });
        }
    }
}
