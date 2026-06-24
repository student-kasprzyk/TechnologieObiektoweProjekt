namespace GraTerenowa.Services;

public class GameStateService
{
    public int ActiveSetId
    {
        get => Preferences.Get("ActiveSetId", -1);
        set => Preferences.Set("ActiveSetId", value);
    }

    private readonly HashSet<string> _completedPins = [];

    public bool IsPinCompleted(string pinTaskId)
        => _completedPins.Contains(pinTaskId);

    public void MarkPinCompleted(string pinTaskId)
    {
        _completedPins.Add(pinTaskId);
        PinCompleted?.Invoke(this, pinTaskId);
    }

    public event EventHandler<string>? PinCompleted;
}