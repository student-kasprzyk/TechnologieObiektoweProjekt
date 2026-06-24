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
    private bool _isCreatorMode;

    private readonly Dictionary<CustomPin, Marker> _pinMarkerMap = new();
    private readonly Dictionary<string, CustomPin> _markerIdToPinMap = new();

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
      _googleMap.UiSettings.ZoomControlsEnabled = false;
      _googleMap.UiSettings.MyLocationButtonEnabled = false;
      _googleMap.UiSettings.CompassEnabled = false;

      _googleMap.MapClick += (s, e) =>
          MapInteractionService.RaiseMapTapped(new Location(e.Point.Latitude, e.Point.Longitude));

      _googleMap.MarkerDragEnd += (s, e) =>
      {
        if (_markerIdToPinMap.TryGetValue(e.Marker.Id, out var pin))
          pin.Location = new Location(e.Marker.Position.Latitude, e.Marker.Position.Longitude);
      };

      UpdateValue(nameof(MauiMap.Pins));
    }

    public void SetCreatorMode(bool isCreator)
    {
      _isCreatorMode = isCreator;
      foreach (var marker in _pinMarkerMap.Values)
        marker.Draggable = isCreator;
    }

    public void OnCustomPinPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
      if (e.PropertyName == nameof(CustomPin.ImageSource) && sender is CustomPin pin)
        MainThread.BeginInvokeOnMainThread(() => UpdateMarkerIcon(pin));
    }

    private void UpdateMarkerIcon(CustomPin pin)
    {
      if (_googleMap == null) return;
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

      foreach (var pin in mapHandler._pinMarkerMap.Keys)
        pin.PropertyChanged -= mapHandler.OnCustomPinPropertyChanged;

      foreach (var marker in mapHandler._pinMarkerMap.Values)
        marker.Remove();

      mapHandler._pinMarkerMap.Clear();
      mapHandler._markerIdToPinMap.Clear();

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
        options.Draggable(_isCreatorMode);

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
            _markerIdToPinMap[marker.Id] = customPin;
            customPin.PropertyChanged += OnCustomPinPropertyChanged;
          }
        }
      }
    }
  }
}
