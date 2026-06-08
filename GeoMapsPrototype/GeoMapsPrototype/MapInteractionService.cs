namespace GeoMapsPrototype
{
    internal class MapInteractionService
    {
        public static event EventHandler<Location>? MapTapped;
        public static void RaiseMapTapped(Location location) => MapTapped?.Invoke(null, location);
    }
}
