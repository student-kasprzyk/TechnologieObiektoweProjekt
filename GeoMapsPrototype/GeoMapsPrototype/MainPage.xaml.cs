using Map = Microsoft.Maui.Controls.Maps.Map;

namespace GeoMapsPrototype
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            Map map = new Map();
            Content = map;
            map.IsShowingUser = true;
        }

    }
}
