using GraTerenowa.Services;
using GraTerenowa.ViewModels;

namespace GraTerenowa.Views;

[QueryProperty(nameof(SetId), "SetId")]
[QueryProperty(nameof(PinTaskId), "PinTaskId")]
public partial class TaskDetailPage : ContentPage
{
    private readonly TaskViewModel _vm;
    private readonly GameStateService _gameState;
    private string _pinTaskId = string.Empty;

    public int SetId
    {
        set => _ = _vm.LoadTasksAsync(value);
    }

    public string PinTaskId
    {
        set => _pinTaskId = value ?? string.Empty;
    }

    public TaskDetailPage(TaskViewModel vm, GameStateService gameState)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
        _gameState = gameState;

        _vm.SetSubmitted += () =>
        {
            // Oznacz pin tylko jeśli przyszliśmy z mapy
            if (!string.IsNullOrEmpty(_pinTaskId))
                _gameState.MarkPinCompleted(_pinTaskId);
        };
    }
}