using GraTerenowa.ViewModels;

namespace GraTerenowa.Views;

[QueryProperty(nameof(SetId), "SetId")]
[QueryProperty(nameof(TaskId), "TaskId")]
[QueryProperty(nameof(SortOrder), "SortOrder")]
public partial class TaskEditorPage : ContentPage
{
    private readonly TaskEditorViewModel _vm;

    // Parametry nawigacji Shell 

    public int SetId
    {
        set => _vm.ActiveSetId = value;
    }

    public int TaskId
    {
        set
        {
            if (value > 0)
                _ = _vm.LoadTaskAsync(value);
        }
    }

    public int SortOrder
    {
        set => _vm.NextSortOrder = value;
    }

    // Konstruktor 
    public TaskEditorPage(TaskEditorViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    //  Cykl życia strony 

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Jeśli nie ładuje istniejącego zadania — wyczyść formularz
        if (!_vm.IsEditing)
            _vm.Reset(_vm.ActiveSetId, _vm.NextSortOrder);
    }
}