using Android.Gms.Maps;
using System.Reflection;

namespace GeoMapsPrototype.Platforms.Android
{
    internal class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        private readonly CustomMapHandler _handler;

        public MapReadyCallback(CustomMapHandler handler)
        {
            _handler = handler;
        }

        public async void OnMapReady(GoogleMap googleMap)
        {
            _handler.OnMapReady(googleMap);

            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("styleLight.json");
                using var reader = new StreamReader(stream);
                string json = await reader.ReadToEndAsync();

                googleMap.SetMapStyle(new global::Android.Gms.Maps.Model.MapStyleOptions(json));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd ładowania stylu mapy: {ex.Message}");
            }
        }
    }
}
