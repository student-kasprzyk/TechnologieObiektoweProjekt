using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace GeoMapsPrototype
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            Location BudD = new Location(50.879611, 20.640667); //Test lokalizacja

            Map map = new Map();
            Content = map;
            map.IsShowingUser = true;
            map.IsScrollEnabled = false;
            map.IsZoomEnabled = true;

            CustomPin pin = new CustomPin
            {
                Label = "Test Lokacja",
                Address = "Budynek D",
                Type = PinType.Place,
                Location = BudD,
                ImageSource = ImageSource.FromUri(new Uri("https://weaii.tu.kielce.pl/wp-content/uploads/2016/11/weaii_1.png"))
            };

            map.Pins.Add(pin);

            LocationProvider.LocationChanged += (sender, location) =>
            {
                if (location == null) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (map != null && this.Handler != null)
                    {
                        try
                        {
                            await Task.Delay(100);
                            map.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.5)));
                            System.Diagnostics.Debug.WriteLine($"[UI] Mapa przesunięta na: {location.Latitude}, {location.Longitude}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[UI] Błąd przesunięcia: {ex.Message}");
                        }
                    }
                });
            };
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
        }

    }
}
