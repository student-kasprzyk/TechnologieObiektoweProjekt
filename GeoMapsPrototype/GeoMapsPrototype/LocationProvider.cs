using System;
using System.Collections.Generic;
using System.Text;

namespace GeoMapsPrototype
{
    internal class LocationProvider
    {
        public static event EventHandler<Location> LocationChanged;

        public static void UpdateLocation(Location location)
        {
            LocationChanged?.Invoke(null, location);
        }
    }
}
