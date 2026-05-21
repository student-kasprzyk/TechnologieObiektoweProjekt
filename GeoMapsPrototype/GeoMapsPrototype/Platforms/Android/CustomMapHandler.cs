using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using MauiMap = Microsoft.Maui.Maps.IMap;
using MauiMapHandler = Microsoft.Maui.Maps.Handlers.IMapHandler;

namespace GeoMapsPrototype.Platforms.Android
{
    public class CustomMapHandler : MapHandler
    {
        public static readonly IPropertyMapper<MauiMap, MauiMapHandler> CustomMapper =
            new PropertyMapper<MauiMap, MauiMapHandler>(Mapper)
            {
                [nameof(MauiMap.Pins)] = MapPins
            };

        public CustomMapHandler() : base(CustomMapper, CommandMapper) { }

        private GoogleMap? _googleMap;

        // Słownik zamiast listy – lookup po referencji pinu, bez kruchego porównania string == object?
        private readonly Dictionary<CustomPin, Marker> _pinMarkerMap = new();

        // Zachowane dla kompatybilności z MapReadyCallback
        public List<Marker> Markers => _pinMarkerMap.Values.ToList();

        protected override void ConnectHandler(MapView platformView)
        {
            base.ConnectHandler(platformView);
            PlatformView.GetMapAsync(new MapReadyCallback(this));
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;
            _googleMap.UiSettings.MapToolbarEnabled = false;
            _googleMap.UiSettings.ZoomControlsEnabled = true;
            UpdateValue(nameof(MauiMap.Pins));
        }

        // Wywoływany przez PropertyChanged na każdym CustomPin
        public void OnCustomPinPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CustomPin.ImageSource) && sender is CustomPin pin)
            {
                MainThread.BeginInvokeOnMainThread(() => UpdateMarkerIcon(pin));
            }
        }

        private void UpdateMarkerIcon(CustomPin pin)
        {
            if (_googleMap == null) return;

            // Bezpośredni lookup po referencji – żadnego porównania string == object?
            if (!_pinMarkerMap.TryGetValue(pin, out var marker)) return;

            if (pin.ImageSource is FileImageSource fis)
            {
                var resId = Context.Resources.GetIdentifier(
                    fis.File.Replace(".png", ""), "drawable", Context.PackageName);

                if (resId != 0)
                    marker.SetIcon(BitmapDescriptorFactory.FromResource(resId));
            }
        }

        private static new void MapPins(MauiMapHandler handler, MauiMap map)
        {
            if (handler is not CustomMapHandler mapHandler) return;

            // Odsubskrybuj stare piny przed wyczyszczeniem
            foreach (var pin in mapHandler._pinMarkerMap.Keys)
                pin.PropertyChanged -= mapHandler.OnCustomPinPropertyChanged;

            foreach (var marker in mapHandler._pinMarkerMap.Values)
                marker.Remove();

            mapHandler._pinMarkerMap.Clear();

            mapHandler.AddPins(map.Pins);
        }

        private void AddPins(IEnumerable<IMapPin> mapPins)
        {
            if (_googleMap == null || MauiContext == null) return;

            foreach (var pin in mapPins)
            {
                var options = new MarkerOptions();
                options.SetPosition(new LatLng(pin.Location.Latitude, pin.Location.Longitude));
                options.SetTitle(pin.Label);

                if (pin is CustomPin cp && cp.ImageSource is FileImageSource fis)
                {
                    var resId = Context.Resources.GetIdentifier(
                        fis.File.Replace(".png", ""), "drawable", Context.PackageName);
                    if (resId != 0)
                        options.InvokeIcon(BitmapDescriptorFactory.FromResource(resId));
                }

                var marker = _googleMap.AddMarker(options);
                if (marker != null)
                {
                    pin.MarkerId = marker.Id;

                    if (pin is CustomPin customPin)
                    {
                        _pinMarkerMap[customPin] = marker;
                        customPin.PropertyChanged += OnCustomPinPropertyChanged;
                    }
                }
            }
        }
    }
}