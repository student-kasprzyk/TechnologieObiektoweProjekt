using System.Runtime.CompilerServices;

namespace GeoMapsPrototype;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(FlyoutIsPresented) && FlyoutIsPresented)
        {
            RefreshNavigationMenu();
        }
    }

    private void RefreshNavigationMenu()
    {
        // 1. Czyścimy dynamiczne elementy
        var toRemove = Items.Where(i => (object)i is MenuItem).ToList();
        foreach (var item in toRemove) Items.Remove(item);

        if (Current?.CurrentPage is MainPage mainPage)
        {
            // 2. Dodajemy przycisk zakończenia, jeśli nawigacja trwa
            if (mainPage.IsNavigating)
            {
                var stopItem = new MenuItem
                {
                    Text = "ZAKOŃCZ NAWIGACJĘ",
                    Command = new Command(() => {
                        FlyoutIsPresented = false;
                        mainPage.StopNavigation();
                    })
                };
                Items.Add(stopItem);
            }

            // 3. Dodajemy listę celów (piny)
            if (mainPage.MapInstance != null)
            {
                var uniquePins = mainPage.MapInstance.Pins
                    .GroupBy(p => p.Label)
                    .Select(g => g.First());

                foreach (var pin in uniquePins)
                {
                    Items.Add(new MenuItem
                    {
                        Text = $"Cel: {pin.Label}",
                        Command = new Command(async () => {
                            FlyoutIsPresented = false;
                            await mainPage.StartNavigation(pin.Location);
                        })
                    });
                }
            }
        }
    }
}