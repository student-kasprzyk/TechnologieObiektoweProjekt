using System;
using System.Collections.Generic;
using System.Text;

namespace GeoMapsPrototype
{
    internal class LocationProvider
    {
        private static readonly WeakEventManager eventManager = new WeakEventManager();

        public static event EventHandler<Location> LocationChanged
        {
            add => eventManager.AddEventHandler(value);
            remove => eventManager.RemoveEventHandler(value);
        }
        public static void UpdateLocation(Location location)
        {
            eventManager.HandleEvent(null, location, nameof(LocationChanged));
        }
    }
}
