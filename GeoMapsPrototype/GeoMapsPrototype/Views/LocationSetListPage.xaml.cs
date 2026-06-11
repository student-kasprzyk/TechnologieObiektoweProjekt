using GraTerenowa.ViewModels;

namespace GraTerenowa.Views;

public partial class LocationSetListPage : ContentPage
{
    private readonly LocationSetViewModel _vm;

    public LocationSetListPage(LocationSetViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadSetsAsync();
    }
}