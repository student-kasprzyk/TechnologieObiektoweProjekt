using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics.Drawables;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;
using System;
using System.Collections.Generic;
using System.Text;
using MauiMap = Microsoft.Maui.Maps.IMap;
using MauiMapHandler = Microsoft.Maui.Maps.Handlers.IMapHandler;
using Microsoft.Maui.Platform;

namespace GeoMapsPrototype.Platforms.Android
{
    public class CustomMapHandler : MapHandler
    {
        public static readonly IPropertyMapper<MauiMap, MauiMapHandler> CustomMapper =
            new PropertyMapper<MauiMap, MauiMapHandler>(Mapper)
            {
                [nameof(MauiMap.Pins)] = MapPins,
            };

        public CustomMapHandler() : base(CustomMapper, CommandMapper)
        {
        }

        public CustomMapHandler(IPropertyMapper? mapper = null, CommandMapper? commandMapper = null) : base(
            mapper ?? CustomMapper, commandMapper ?? CommandMapper)
        {
        }

        private GoogleMap? _googleMap;
        public List<Marker> Markers { get; } = new();

        protected override void ConnectHandler(MapView platformView)
        {
            base.ConnectHandler(platformView);
            var mapReady = new MapReadyCallback(this);
            PlatformView.GetMapAsync(mapReady);
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            _googleMap = googleMap;
        }

        private static new void MapPins(MauiMapHandler handler, MauiMap map)
        {
            if (handler is CustomMapHandler mapHandler)
            {
                foreach (var marker in mapHandler.Markers)
                {
                    marker.Remove();
                }

                mapHandler.AddPins(map.Pins);
            }
        }

        private void AddPins(IEnumerable<IMapPin> mapPins)
        {
            if (Map is null || MauiContext is null)
            {
                return;
            }

            foreach (var pin in mapPins)
            {
                var pinHandler = pin.ToHandler(MauiContext);
                if (pinHandler is IMapPinHandler mapPinHandler)
                {
                    var markerOption = mapPinHandler.PlatformView;
                    if (_googleMap != null)
                        if (pin is CustomPin cp)
                        {
                            cp.ImageSource.LoadImage(MauiContext, result =>
                            {
                                if (result?.Value is BitmapDrawable bitmapDrawable && bitmapDrawable.Bitmap != null)
                                {
                                    markerOption.SetIcon(BitmapDescriptorFactory.FromBitmap(bitmapDrawable.Bitmap));
                                }

                                AddMarker(_googleMap, pin, Markers, markerOption);
                            });
                        }
                        else
                        {
                            AddMarker(_googleMap, pin, Markers, markerOption);
                        }
                }
            }
        }

        private static void AddMarker(GoogleMap? map, IMapPin pin, List<Marker> markers, MarkerOptions markerOption)
        {
            if (map is null) return;
            var marker = map.AddMarker(markerOption);
            if (marker is not null)
            {
                pin.MarkerId = marker.Id;
                markers.Add(marker);
            }
        }
    }
}
