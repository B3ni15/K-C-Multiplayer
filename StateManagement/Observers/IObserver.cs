using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static KCM.StateManagement.Observers.Observer;

namespace KCM.StateManagement.Observers
{
    public interface IObserver
    {
        List<FieldInfo> monitoredFields { get; set; }
        List<PropertyInfo> monitoredProperties { get; set; }

        int updateInterval { get; set; }
        long lastUpdate { get; set; }
        Dictionary<string, object> values { get; set; }
        GameObject observerObject { get; set; }

        void Initialise<T>(T instance, string[] monitoredFields, GameObject observerObject, int updateInterval);

        void Update();

        void StateChanged(string name, object value);

        EventHandler<StateUpdateEventArgs> StateUpdated { get; set; }
    }
}
