using Language.Lua;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static UnityEngine.PlayerLoop.PreUpdate;

namespace KCM.StateManagement.Observers
{
    public class Observer : MonoBehaviour, IObserver
    {
        public object state { get; set; }

        public List<FieldInfo> monitoredFields { get; set; }
        public List<PropertyInfo> monitoredProperties { get; set; }
        public long lastUpdate { get; set; }
        public int updateInterval { get; set; }
        public long currentMs => DateTimeOffset.Now.ToUnixTimeMilliseconds();

        public Dictionary<string, object> values { get; set; }
        public Dictionary<string, object> changedValues { get; set; }

        public GameObject observerObject { get; set; }
        public EventHandler<StateUpdateEventArgs> StateUpdated { get; set; }
        public EventHandler<StateUpdateEventArgs> SendUpdate { get; set; }



        public long lastPacket { get; set; }
        public int packetInterval = 300;

        public class ListTypeVariables
        {
            public List<FieldInfo> fields;
            public List<PropertyInfo> properties;

            public ListTypeVariables(Type type)
            {

                fields = new List<FieldInfo>();
                properties = new List<PropertyInfo>();

                var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                foreach (var field in type.GetFields(bindingFlags).OrderBy(field => field.Name))
                    fields.Add(field);

                foreach (var prop in type.GetProperties(bindingFlags).OrderBy(prop => prop.Name))
                    properties.Add(prop);

                Main.helper.Log($"ListTypeVariables: {type.Name} has {fields.Count} fields and {properties.Count} properties");
            }
        }

        public Dictionary<string, ListTypeVariables> listVariables = new Dictionary<string, ListTypeVariables>();

        public void Initialise<T>(T state, string[] monitoredFields, GameObject observerObject, int updateInterval = 100)
        {
            this.state = state;
            this.monitoredFields = new List<FieldInfo>();
            this.monitoredProperties = new List<PropertyInfo>();
            this.lastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            this.observerObject = observerObject;
            this.updateInterval = updateInterval;
            this.values = new Dictionary<string, object>();
            this.changedValues = new Dictionary<string, object>();

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            foreach (var field in monitoredFields)
            {
                if (state.GetType().GetField(field, bindingFlags) != null)
                    this.monitoredFields.Add(state.GetType().GetField(field, bindingFlags));
                else if (state.GetType().GetProperty(field, bindingFlags) != null)
                    this.monitoredProperties.Add(state.GetType().GetProperty(field, bindingFlags));
            }


            // This will store all the fields and properties of the list type on start for efficient access and later comparison
            foreach (var field in this.monitoredFields)
            {

                if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = field.FieldType.GetGenericArguments()[0];

                    listVariables.Add(field.Name, new ListTypeVariables(listType));
                }

                if (field.FieldType.IsGenericType &&
                  field.FieldType.GetGenericTypeDefinition() == typeof(ArrayExt<>))
                {
                    var listType = field.FieldType.GetField("data").FieldType.GetElementType();
                    listVariables.Add(field.Name, new ListTypeVariables(listType));
                }
            }

            foreach (var prop in this.monitoredProperties)
            {

                if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var listType = prop.PropertyType.GetGenericArguments()[0];

                    if (listType.IsPrimitive)
                        continue;

                    listVariables.Add(prop.Name, new ListTypeVariables(listType));
                }

                if (prop.PropertyType.IsGenericType &&
                  prop.PropertyType.GetGenericTypeDefinition() == typeof(ArrayExt<>))
                {
                    var listType = prop.PropertyType.GetField("data").FieldType.GetElementType();
                    listVariables.Add(prop.Name, new ListTypeVariables(listType));
                }
            }

            Main.helper.Log($"Observer created for {state.GetType().Name} with {this.monitoredFields.Count} fields, {this.monitoredProperties.Count} properties, and {listVariables.Count} non-primitive list variables");
        }

        public void Update()
        {
            if (this.state == null)
                return;

            if (!(currentMs - lastUpdate > updateInterval)) // Don't run if the update interval hasn't passed (default 100 milliseconds);
                return;

            foreach (var field in monitoredFields)
                UpdateValue(field.Name, field.GetValue(state));

            foreach (var prop in monitoredProperties)
                UpdateValue(prop.Name, prop.GetValue(state));



            if ((currentMs - lastPacket > packetInterval) && changedValues.Count > 0)
            {
                try
                {
                    SendUpdate?.Invoke(this, null);
                    lastPacket = currentMs;
                    changedValues.Clear();
                }                 catch (Exception e)
                {
                    Main.helper.Log($"Error sending update: {e.Message}");
                    Main.helper.Log($"Stack trace: {e.StackTrace}");
                }
            }
        }

        public void UpdateValue(string name, object value)
        {
            if (values.ContainsKey(name))
            {
                if (!AreEqual(name, values[name], value))
                {
                    StateChanged(name, value);

                    if (isArrayExt(values[name]))
                        values[name].GetType().GetField("data").SetValue(values[name], value);
                    else
                        values[name] = value;

                    this.lastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (isArrayExt(values[name]))
                        changedValues[name].GetType().GetField("data").SetValue(values[name], value);
                    else
                        changedValues[name] = value;
                }
            }
            else
            {
                if (!changedValues.ContainsKey(name))
                    changedValues.Add(name, value);

                values.Add(name, value);
                StateChanged(name, value);
            }
        }

        public class StateUpdateEventArgs : EventArgs
        {
            public string name;
            public object value;
        }

        public void StateChanged(string name, object value)
        {
            StateUpdated?.Invoke(this, new StateUpdateEventArgs { name = name, value = value });

            //Main.helper.Log($"{name} state changed to {value}");
        }

        bool IsListButNotOfPrimitives(object obj)
        {
            return obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition() == typeof(List<>) &&
                   !obj.GetType().GetGenericArguments()[0].IsPrimitive;
        }

        bool isArrayExt(object obj)
        {
            return obj.GetType().IsGenericType &&
                   obj.GetType().GetGenericTypeDefinition() == typeof(ArrayExt<>);
        }

        object[] ToObjectArray(object obj)
        {
            return ((IEnumerable)obj).Cast<object>().ToArray();
        }

        public bool AreEqual(string fieldName, object a, object b)
        {
            object current = b;
            object previous = a;

            if (isArrayExt(a) && isArrayExt(b))
            {
                object dataA = a.GetType().GetField("data").GetValue(previous);
                object dataB = b.GetType().GetField("data").GetValue(current);


                return DeepArrayTypeEqualsCheck(fieldName, (Array)dataA, (Array)dataB);
            }

            if (IsListButNotOfPrimitives(a) && IsListButNotOfPrimitives(b))
            {
                var aAsObjectArray = ToObjectArray(a);
                var bAsObjectArray = ToObjectArray(b);

                return DeepArrayTypeEqualsCheck(fieldName, aAsObjectArray, bAsObjectArray);
            }

            if ((current.GetType().IsArray && previous.GetType().IsArray) && !(current.GetType().IsPrimitive && previous.GetType().IsPrimitive))
                return DeepArrayTypeEqualsCheck(fieldName, (Array)a, (Array)b);

            // Check if both are null or are the same instance
            if (ReferenceEquals(a, b)) return true;

            if (a == null || b == null) return false;

            if (current.GetType().IsArray && previous.GetType().IsArray)
            {
                // Check for single-dimensional arrays
                if (current is Array aArray && previous is Array bArray)
                {
                    // Different lengths mean they are not equal
                    if (aArray.Length != bArray.Length) return false;

                    // Handle 2D arrays specifically
                    if (aArray.Rank == 2 && bArray.Rank == 2)
                    {
                        return Are2DArraysEqual(aArray, bArray);
                    }
                    // Handle 1D arrays
                    else if (aArray.Rank == 1 && bArray.Rank == 1)
                    {
                        return Enumerable.SequenceEqual(aArray.Cast<object>(), bArray.Cast<object>());
                    }
                }
            }

            // Fallback to default Equals for other types
            return Equals(current, previous);
        }

        private bool Are2DArraysEqual(Array a, Array b)
        {
            if (a.GetLength(0) != b.GetLength(0) || a.GetLength(1) != b.GetLength(1)) return false;

            for (int i = 0; i < a.GetLength(0); i++)
            {
                for (int j = 0; j < a.GetLength(1); j++)
                {
                    if (!Equals(a.GetValue(i, j), b.GetValue(i, j))) return false;
                }
            }

            return true;
        }

        private bool DeepArrayTypeEqualsCheck(string varName, Array a, Array b)
        {
            try
            {
                // Check for reference equality and nulls
                if (ReferenceEquals(a, b)) return true;
                if (a == null || b == null) return false;

                // Compare counts
                if (a.Length != b.Length) return false;

                for (int i = 0; i < a.Length; i++)
                {
                    foreach (var field in listVariables[varName].fields)
                    {
                        if (!Equals(field.GetValue(a.GetValue(i)), field.GetValue(b.GetValue(i))))
                            return false;
                    }

                    foreach (var prop in listVariables[varName].properties)
                    {
                        if (!Equals(prop.GetValue(a.GetValue(i)), prop.GetValue(b.GetValue(i))))
                            return false;
                    }
                }
            }
            catch (Exception e)
            {
                Main.helper.Log($"Error comparing {varName} arrays: {e.Message}");
                return true;
            }

            return true;
        }

    }
}
