//using Android.App;
using GraTerenowa.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using System.Text.Json;
using Map = Microsoft.Maui.Controls.Maps.Map;


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

        private bool isCreatorMode = false;

        private DateTime lastMoveTime = DateTime.MinValue;
        private DateTime lastRouteUpdateTime = DateTime.MinValue;

        private Button? _fabButton;
        private Label? _creatorBadge;
        private Button? _saveButton;
        private Button? _loadButton;

        private readonly GameStateService _gameState;

        public MainPage(GameStateService gameState)
        {
            _gameState = gameState;

            _gameState.PinCompleted += (_, pinTaskId) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (map == null) return;
                    var pin = map.Pins
                        .OfType<CustomPin>()
                        .FirstOrDefault(p => p.TaskData?.TaskId == pinTaskId);
                    if (pin is null) return;
                    pin.IsTaskCompleted = true;
                    pin.ImageSource = ImageSource.FromFile("pin_blue.png");
                });
            };
            InitializeComponent();

            map = new Map { IsShowingUser = true, IsScrollEnabled = false, IsZoomEnabled = true };

            _creatorBadge = new Label
            {
                Text = "TRYB TWORZENIA",
                BackgroundColor = Color.FromArgb("#CC1565C0"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Start,
                Padding = new Thickness(16, 8),
                Margin = new Thickness(0, 40, 0, 0),
                IsVisible = false
            };

            _fabButton = new Button
            {
                Text = "+",
                WidthRequest = 56,
                HeightRequest = 56,
                CornerRadius = 28,
                BackgroundColor = Color.FromArgb("#1565C0"),
                TextColor = Colors.White,
                FontSize = 20,
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 0, 16, 32)
            };
            _fabButton.Clicked += OnFabClicked;

            _saveButton = new Button
            {
                Text = "Zapisz mapę",
                BackgroundColor = Color.FromArgb("#CC2E7D32"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(16, 0, 0, 32),
                CornerRadius = 8,
                IsVisible = false
            };
            _saveButton.Clicked += OnSaveClicked;

            _loadButton = new Button
            {
                Text = "Wczytaj mapę",
                BackgroundColor = Color.FromArgb("#CC6A1E0A"),
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(16, 0, 0, 88),
                CornerRadius = 8,
                IsVisible = false
            };
            _loadButton.Clicked += OnLoadClicked;

            var overlayGrid = new Grid();
            overlayGrid.Children.Add(map);
            overlayGrid.Children.Add(_creatorBadge);
            overlayGrid.Children.Add(_fabButton);
            overlayGrid.Children.Add(_saveButton);
            overlayGrid.Children.Add(_loadButton);

            Content = overlayGrid;

            AddInitialCustomPin();

            MapInteractionService.MapTapped += async (s, location) =>
            {
                if (!isCreatorMode) return;
                await AddPinAtLocation(location);
            };

            LocationProvider.LocationChanged += (sender, location) =>
            {
                if (location == null) return;
                lastUserLocation = location;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (map == null || this.Handler == null) return;

                    foreach (var pin in map.Pins.OfType<CustomPin>())
                    {
                        if (pin.IsTaskCompleted) continue;
                        double distance = location.CalculateDistance(pin.Location, DistanceUnits.Kilometers) * 1000;
                        var targetFile = (distance <= pin.Radius) ? "pin_green.png" : "pin_red.png";

                        if (pin.ImageSource is not FileImageSource fis || fis.File != targetFile)
                            pin.ImageSource = ImageSource.FromFile(targetFile);
                    }

                    if (isNavigating && currentDestination != null)
                    {
                        if ((DateTime.Now - lastRouteUpdateTime).TotalSeconds > 30)
                        {
                            await FetchActualRoute(location, currentDestination);
                            lastRouteUpdateTime = DateTime.Now;
                        }

                        if (map.MapElements.OfType<Polyline>().FirstOrDefault() is Polyline line && line.Geopath.Count > 0)
                        {
                            line.Geopath.RemoveAt(0);
                            line.Geopath.Insert(0, location);
                        }
                    }

                    bool isFlyoutOpen = Shell.Current?.FlyoutIsPresented ?? false;
                    if (!isCreatorMode && !isFlyoutOpen && (DateTime.Now - lastMoveTime).TotalSeconds > 4)
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

        private void OnFabClicked(object? sender, EventArgs e)
        {
            isCreatorMode = !isCreatorMode;

            if (map != null)
                map.IsScrollEnabled = isCreatorMode;

            if (_creatorBadge != null)
                _creatorBadge.IsVisible = isCreatorMode;

            if (_saveButton != null)
                _saveButton.IsVisible = isCreatorMode;

            if (_loadButton != null)
                _loadButton.IsVisible = isCreatorMode;

            if (_fabButton != null)
            {
                _fabButton.Text = isCreatorMode ? "X" : "+";
                _fabButton.BackgroundColor = isCreatorMode
                    ? Color.FromArgb("#D32F2F")
                    : Color.FromArgb("#1565C0");
            }

#if ANDROID
            if (map?.Handler is Platforms.Android.CustomMapHandler handler)
                handler.SetCreatorMode(isCreatorMode);
#endif

            if (isCreatorMode && isNavigating)
                StopNavigation();
        }

        private async Task AddPinAtLocation(Location location)
        {
            var label = await DisplayPromptAsync("Nowy punkt", "Nazwa punktu:", "OK", "Anuluj");
            if (string.IsNullOrWhiteSpace(label)) return;

            var radiusStr = await DisplayPromptAsync("Promień", "Promień aktywacji (metry):",
                "OK", "Anuluj", initialValue: "50", keyboard: Keyboard.Numeric);
            if (!double.TryParse(radiusStr, out double radius)) radius = 50;

            var taskDesc = await DisplayPromptAsync("Zadanie", "Opis zadania (opcjonalnie):",
                "OK", "Pomiń", initialValue: string.Empty);

            var pin = new CustomPin
            {
                Label = label,
                Address = string.Empty,
                Location = location,
                Radius = radius,
                ImageSource = ImageSource.FromFile("pin_red.png"),
                IsTaskCompleted = false,
                TaskData = new PinTask { Description = taskDesc ?? string.Empty }
            };

            pin.MarkerClicked += OnPinMarkerClicked;
            map!.Pins.Add(pin);
        }

        private async Task EditPin(CustomPin pin)
        {
            var label = await DisplayPromptAsync("Edytuj punkt", "Nazwa punktu:",
                "OK", "Anuluj", initialValue: pin.Label);
            if (string.IsNullOrWhiteSpace(label)) return;

            var radiusStr = await DisplayPromptAsync("Promień", "Promień aktywacji (metry):",
                "OK", "Anuluj", initialValue: pin.Radius.ToString("0"), keyboard: Keyboard.Numeric);
            if (!double.TryParse(radiusStr, out double radius)) radius = pin.Radius;

            var taskDesc = await DisplayPromptAsync("Zadanie", "Opis zadania:",
                "OK", "Anuluj", initialValue: pin.TaskData?.Description ?? string.Empty);

            pin.Label = label;
            pin.Radius = radius;
            if (pin.TaskData == null) pin.TaskData = new PinTask();
            pin.TaskData.Description = taskDesc ?? string.Empty;
        }

        private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (map == null || !map.Pins.OfType<CustomPin>().Any())
            {
                await DisplayAlert("Brak danych", "Dodaj co najmniej jeden punkt przed zapisem.", "OK");
                return;
            }

            var name = await DisplayPromptAsync("Zapisz mapę", "Nazwa mapy:", "Zapisz", "Anuluj");
            if (string.IsNullOrWhiteSpace(name)) return;

            MapStorageService.Save(name, map.Pins.OfType<CustomPin>());
            await DisplayAlert("Zapisano", $"Mapa \"{name}\" została zapisana.", "OK");
        }

        private async void OnLoadClicked(object? sender, EventArgs e)
        {
            var maps = MapStorageService.GetSavedMaps();
            if (maps.Count == 0)
            {
                await DisplayAlert("Brak map", "Nie ma żadnych zapisanych map.", "OK");
                return;
            }

            var selected = await DisplayActionSheet("Wybierz mapę", "Anuluj", null, maps.ToArray());
            if (string.IsNullOrEmpty(selected) || selected == "Anuluj") return;

            var pins = MapStorageService.Load(selected);
            if (pins == null || map == null) return;

            map.Pins.Clear();
            foreach (var pin in pins)
            {
                pin.MarkerClicked += OnPinMarkerClicked;
                map.Pins.Add(pin);
            }

            await DisplayAlert("Wczytano", $"Mapa \"{selected}\" została wczytana.", "OK");
        }

        private async void OnPinMarkerClicked(object? sender, PinClickedEventArgs e)
        {
            e.HideInfoWindow = true;
            if (sender is not CustomPin cp) return;

            if (isCreatorMode)
            {
                var action = await DisplayActionSheet(cp.Label, "Anuluj", null, "Edytuj", "Usuń");
                if (action == "Edytuj")
                    await EditPin(cp);
                else if (action == "Usuń")
                    map?.Pins.Remove(cp);
                return;
            }

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
            else if (dist <= cp.Radius) //od tąd
            {
                var setId = Preferences.Get("ActiveSetId", -1);

                if (setId == -1)
                {
                    await DisplayAlert(
                        "Brak zestawu",
                        "Przejdź do zakładki 'Zestawy zadań' i wybierz zestaw.",
                        "OK");
                    return;
                }

                await Shell.Current.GoToAsync(
                    nameof(GraTerenowa.Views.TaskDetailPage),
                    new Dictionary<string, object>
                    {
                        ["SetId"] = setId,
                        ["PinTaskId"] = cp.TaskData?.TaskId ?? cp.Label
                    });
            } // do tąd
            else
            {
                await StartNavigation(cp.Location);
            }
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
                    travelMode = "WALK"
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
                IsTaskCompleted = false,
                TaskData = new PinTask { Description = "Tutaj można wstawić zadanie!" }
            };

            testPin.MarkerClicked += OnPinMarkerClicked;
            map.Pins.Add(testPin);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }
    }
}