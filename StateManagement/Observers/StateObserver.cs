using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static KCM.StateManagement.Observers.Observer;

namespace KCM.StateManagement.Observers
{
    public class StateObserver
    {
        public static Dictionary<int, IObserver> observers = new Dictionary<int, IObserver>();

        public static void RegisterObserver<T>(T instance, string[] monitoredFields, EventHandler<StateUpdateEventArgs> eventHandler = null, EventHandler<StateUpdateEventArgs> sendUpdateHandler = null)
        {
            if (observers.ContainsKey(instance.GetHashCode()))
                return;

            var observerObject = new GameObject($"{instance.GetHashCode()} {instance.GetType().Name} State Observer");

            var observer = observerObject.AddComponent<Observer>();


            observer.Initialise(instance, monitoredFields, observerObject);


            if (eventHandler != null)
                observer.StateUpdated += eventHandler;


            if (sendUpdateHandler != null)
                observer.SendUpdate += sendUpdateHandler;


            observers.Add(instance.GetHashCode(), observer);

        }
    }
}
