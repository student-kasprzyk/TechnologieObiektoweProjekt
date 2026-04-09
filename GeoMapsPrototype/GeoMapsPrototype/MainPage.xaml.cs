using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace GeoMapsPrototype
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            Location BudD = new Location(50.879611, 20.640667); //Test lokalizacja
            MapSpan mapSpan = new MapSpan(BudD, 0.01, 0.01);

            Map map = new Map(mapSpan);
            Content = map;
            map.IsShowingUser = true;
            map.IsScrollEnabled = false;
            map.IsZoomEnabled = true;//Poszukać wsparcia dla przybliżenia ale z zablokowaniem dla klienta (opcjonalne)

            //Przybliżenie mapy
            double zoomLevel = 10;
            double latlongDegrees = 360 / (Math.Pow(2, zoomLevel));
            if (map.VisibleRegion != null)
            {
                map.MoveToRegion(new MapSpan(map.VisibleRegion.Center, latlongDegrees, latlongDegrees));
            }

            /*CustomPin pin = new CustomPin
            {
                Label = "Test Lokacja",
                Address = "Budynek D",
                Type = PinType.Place,
                Location = BudD,
                ImageSource = ImageSource.FromUri(new Uri("https://weaii.tu.kielce.pl/wp-content/uploads/2016/11/weaii_1.png"))
            };*/

            Pin pin = new Pin
            {
                Label = "Test Lokacja",
                Address = "Budynek D",
                Type = PinType.Place,
                Location = BudD,
            };
            map.Pins.Add(pin);
        }

    }
}
