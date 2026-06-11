using GraTerenowa.ViewModels;

namespace GraTerenowa.Views;

[QueryProperty(nameof(SetId), "SetId")]
public partial class TaskDetailPage : ContentPage
{
    private readonly TaskViewModel _vm;

    public int SetId
    {
        set => _ = _vm.LoadTasksAsync(value);
    }

    public TaskDetailPage(TaskViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }
}