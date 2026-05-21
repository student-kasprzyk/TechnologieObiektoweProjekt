using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;
using System.Text.Json;

namespace GeoMapsPrototype
{
    public partial class MainPage : ContentPage
    {
        private Map? map;
        public Map? MapInstance => map;

        private Location? lastUserLocation;
        private Location? currentDestination;
        private bool isNavigating = false;
        public bool IsNavigating => isNavigating;

        private DateTime lastMoveTime = DateTime.MinValue;
        private DateTime lastRouteUpdateTime = DateTime.MinValue;

        public MainPage()
        {
            InitializeComponent();

            map = new Map { IsShowingUser = true, IsScrollEnabled = false, IsZoomEnabled = true };
            Content = map;

            AddInitialCustomPin();

            LocationProvider.LocationChanged += (sender, location) =>
            {
                if (location == null) return;
                lastUserLocation = location;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (map == null || this.Handler == null) return;

                    // Geofencing i zmiana ikon
                    foreach (var pin in map.Pins.OfType<CustomPin>())
                    {
                        if (pin.IsTaskCompleted) continue;
                        double distance = location.CalculateDistance(pin.Location, DistanceUnits.Kilometers) * 1000;
                        var targetFile = (distance <= pin.Radius) ? "pin_green.png" : "pin_red.png";

                        if (pin.ImageSource is not FileImageSource fis || fis.File != targetFile)
                        {
                            pin.ImageSource = ImageSource.FromFile(targetFile);
                        }
                    }

                    // Nawigacja i Reroute
                    if (isNavigating && currentDestination != null)
                    {
                        if ((DateTime.Now - lastRouteUpdateTime).TotalSeconds > 30)
                        {
                            await FetchActualRoute(location, currentDestination);
                            lastRouteUpdateTime = DateTime.Now;
                        }

                        if (map.MapElements.OfType<Polyline>().FirstOrDefault() is Polyline line && line.Geopath.Count > 0)
                            line.Geopath[0] = location;
                    }

                    // Centrowanie mapy
                    bool isFlyoutOpen = Shell.Current?.FlyoutIsPresented ?? false;
                    if (!isFlyoutOpen && (DateTime.Now - lastMoveTime).TotalSeconds > 4)
                    {
                        try
                        {
                            map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(isNavigating ? 0.3 : 0.5)));
                            lastMoveTime = DateTime.Now;
                        }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                    }
                });
            };
        }

        public async Task StartNavigation(Location destination)
        {
            if (lastUserLocation == null) return;
            isNavigating = true;
            currentDestination = destination;
            lastRouteUpdateTime = DateTime.Now;
            await FetchActualRoute(lastUserLocation, destination);
        }

        public void StopNavigation()
        {
            isNavigating = false;
            currentDestination = null;
            MainThread.BeginInvokeOnMainThread(() => map?.MapElements.Clear());
        }

        private async Task FetchActualRoute(Location start, Location end)
        {
            try
            {
                string apiKey = string.Empty;
#if ANDROID
                var context = Android.App.Application.Context;
                var appInfo = context.PackageManager.GetApplicationInfo(context.PackageName, (Android.Content.PM.PackageInfoFlags)128);
                apiKey = appInfo.MetaData?.GetString("com.google.android.geo.API_KEY") ?? string.Empty;
#endif
                if (string.IsNullOrEmpty(apiKey) || apiKey == "APIKEY") return;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
                client.DefaultRequestHeaders.Add("X-Goog-FieldMask", "routes.polyline.encodedPolyline");

                var body = new
                {
                    origin = new { location = new { latLng = new { latitude = start.Latitude, longitude = start.Longitude } } },
                    destination = new { location = new { latLng = new { latitude = end.Latitude, longitude = end.Longitude } } },
                    travelMode = "DRIVE",
                    routingPreference = "TRAFFIC_AWARE_OPTIMAL"
                };

                var response = await client.PostAsync("https://routes.googleapis.com/directions/v2:computeRoutes",
                    new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("routes", out var routes) && routes.GetArrayLength() > 0)
                    {
                        var encoded = routes[0].GetProperty("polyline").GetProperty("encodedPolyline").GetString();
                        if (!string.IsNullOrEmpty(encoded)) DrawRouteOnMap(DecodePolyline(encoded));
                    }
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        }

        private void DrawRouteOnMap(List<Location> points)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (map == null) return;
                map.MapElements.Clear();
                var line = new Polyline { StrokeColor = Colors.DeepSkyBlue, StrokeWidth = 12 };
                foreach (var p in points) line.Geopath.Add(p);
                map.MapElements.Add(line);
            });
        }

        private List<Location> DecodePolyline(string encoded)
        {
            var poly = new List<Location>();
            int index = 0, lat = 0, lng = 0;
            while (index < encoded.Length)
            {
                int b, shift = 0, result = 0;
                do { b = encoded[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lat += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                shift = 0; result = 0;
                do { b = encoded[index++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
                lng += ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                poly.Add(new Location(lat / 1E5, lng / 1E5));
            }
            return poly;
        }

        private void AddInitialCustomPin()
        {
            if (map == null) return;

            var testPin = new CustomPin
            {
                Label = "Test Lokacja",
                Address = "Budynek D",
                Location = new Location(50.879611, 20.640667),
                Radius = 50.0,
                ImageSource = ImageSource.FromFile("pin_red.png"),
                IsTaskCompleted = false
            };

            testPin.MarkerClicked += async (s, e) =>
            {
                e.HideInfoWindow = true;

                if (s is not CustomPin cp) return;

                if (lastUserLocation == null)
                {
                    await DisplayAlert("Brak GPS", "Poczekaj na ustalenie Twojej lokalizacji.", "OK");
                    return;
                }

                double dist = lastUserLocation.CalculateDistance(cp.Location, DistanceUnits.Kilometers) * 1000;

                if (cp.IsTaskCompleted)
                {
                    await DisplayAlert("Zadanie", "To zadanie zostało już ukończone.", "OK");
                }
                else if (dist <= cp.Radius)
                {
                    await DisplayAlert("Zadanie", "Tutaj można wstawić zadanie!", "OK");
                    cp.IsTaskCompleted = true;
                    cp.ImageSource = ImageSource.FromFile("pin_blue.png");
                }
                else
                {
                    await DisplayAlert("Za daleko", $"Musisz być w odległości {cp.Radius:0} m od celu.\nTeraz jesteś {dist:0} m dalej.", "OK");
                }
            };

            map.Pins.Add(testPin);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
    }
}