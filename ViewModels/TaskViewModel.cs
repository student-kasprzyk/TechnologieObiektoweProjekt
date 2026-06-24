using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraTerenowa.Models;
using GraTerenowa.Models.Payloads;
using GraTerenowa.Services;

namespace GraTerenowa.ViewModels;

public partial class TaskViewModel : ObservableObject
{
    private readonly TaskService _taskService;
    private readonly QRScannerService _scannerService;

    //  Konstruktor 

    public TaskViewModel(
        TaskService taskService,
        QRScannerService scannerService)
    {
        _taskService = taskService;
        _scannerService = scannerService;
    }

    //  Dane bieżącego zadania 

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsQuestion),
        nameof(IsQRCode),
        nameof(IsAction),
        nameof(IsSingleChoice),
        nameof(IsMultipleChoice),
        nameof(QuestionText),
        nameof(ActionDescription))]
    private LocationTask? _currentTask;

    public bool IsQuestion => CurrentTask?.Type == TaskType.Question;
    public bool IsQRCode => CurrentTask?.Type == TaskType.QRCode;
    public bool IsAction => CurrentTask?.Type == TaskType.Action;
    public bool IsSingleChoice => CurrentTask?.Type == TaskType.SingleChoice;
    public bool IsMultipleChoice => CurrentTask?.Type == TaskType.MultipleChoice;

    // Treść zadania odczytana z Payload
    public string QuestionText => CurrentTask is null ? string.Empty
        : _taskService.GetQuestionPayload(CurrentTask)?.Question
          ?? string.Empty;

    public string ActionDescription => CurrentTask is null ? string.Empty
        : _taskService.GetActionPayload(CurrentTask)?.Description
          ?? string.Empty;

    // Stan wykonania 

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private bool _isVerifying;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    //  Lista zadań w zestawie 

    [ObservableProperty]
    private List<LocationTask> _allTasks = [];

    [ObservableProperty]
    private int _currentIndex;

    public bool HasNextTask => CurrentIndex < AllTasks.Count - 1;
    public bool HasPrevTask => CurrentIndex > 0;

    public string ProgressText => AllTasks.Count == 0
        ? string.Empty
        : $"{CurrentIndex + 1} / {AllTasks.Count}";

    //  Pola dla SingleChoice 

    // Zaznaczona opcja A/B/C/D
    [ObservableProperty]
    private string _selectedOption = string.Empty;

    // Opcje do wyświetlenia — ładowane z Payload przy zmianie zadania
    [ObservableProperty]
    private List<ChoiceOption> _currentOptions = [];

    // Tekst pytania dla Choice (odczytany z Payload)
    public string ChoiceQuestionText => CurrentTask?.Type switch
    {
        TaskType.SingleChoice =>
            _taskService.GetSingleChoicePayload(CurrentTask)?.Question
            ?? string.Empty,

        TaskType.MultipleChoice =>
            _taskService.GetMultipleChoicePayload(CurrentTask)?.Question
            ?? string.Empty,

        _ => string.Empty
    };

    //  Pola dla MultipleChoice 

    private bool _isASelected;
    public bool IsASelected
    {
        get => _isASelected;
        set => SetProperty(ref _isASelected, value);
    }

    private bool _isBSelected;
    public bool IsBSelected
    {
        get => _isBSelected;
        set => SetProperty(ref _isBSelected, value);
    }

    private bool _isCSelected;
    public bool IsCSelected
    {
        get => _isCSelected;
        set => SetProperty(ref _isCSelected, value);
    }

    private bool _isDSelected;
    public bool IsDSelected
    {
        get => _isDSelected;
        set => SetProperty(ref _isDSelected, value);
    }

    // Pomocnicza lista zaznaczonych opcji
    private List<string> GetSelectedMultipleOptions() =>
        new[]
        {
            IsASelected ? "A" : null,
            IsBSelected ? "B" : null,
            IsCSelected ? "C" : null,
            IsDSelected ? "D" : null
        }
        .Where(x => x is not null)
        .Select(x => x!)
        .ToList();

    //  Ładowanie zadań zestawu 

    public async Task LoadTasksAsync(int locationSetId)
    {
        AllTasks = await _taskService.GetBySetIdAsync(locationSetId);
        CurrentIndex = 0;

        if (AllTasks.Count > 0)
            await ShowTaskAsync(0);
    }

    private async Task ShowTaskAsync(int index)
    {
        if (index < 0 || index >= AllTasks.Count) return;

        CurrentIndex = index;
        CurrentTask = AllTasks[index];
        UserInput = string.Empty;
        IsCompleted = false;
        IsError = false;
        ResultMessage = string.Empty;

        // Załadowanie opcji dla typów Choice
        LoadChoiceOptions();

        // Sprawdzenie czy zadanie było już zaliczone
        IsCompleted = await _taskService
            .IsTaskCompletedAsync(CurrentTask.Id);

        OnPropertyChanged(nameof(HasNextTask));
        OnPropertyChanged(nameof(HasPrevTask));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ChoiceQuestionText));
        OnPropertyChanged(nameof(SetProgress));
    }

    // Ładuje opcje A/B/C/D z Payload i resetuje zaznaczenie
    private void LoadChoiceOptions()
    {
        if (CurrentTask is null)
        {
            CurrentOptions = [];
            return;
        }

        CurrentOptions = CurrentTask.Type switch
        {
            TaskType.SingleChoice =>
                _taskService.GetSingleChoicePayload(CurrentTask)
                            ?.Options ?? [],

            TaskType.MultipleChoice =>
                _taskService.GetMultipleChoicePayload(CurrentTask)
                            ?.Options ?? [],

            _ => []
        };

        // Reset zaznaczenia
        SelectedOption = string.Empty;
        IsASelected = false;
        IsBSelected = false;
        IsCSelected = false;
        IsDSelected = false;
    }

    //  Komendy nawigacji 

    [RelayCommand]
    private async Task NextTaskAsync()
    {
        if (HasNextTask)
            await ShowTaskAsync(CurrentIndex + 1);
    }

    [RelayCommand]
    private async Task PrevTaskAsync()
    {
        if (HasPrevTask)
            await ShowTaskAsync(CurrentIndex - 1);
    }

    //  Wykonanie zadania

    [RelayCommand]
    private async Task ExecuteTaskAsync()
    {
        if (CurrentTask is null || IsVerifying) return;

        IsVerifying = true;
        IsError = false;
        ResultMessage = string.Empty;

        try
        {
            bool result = CurrentTask.Type switch
            {
                TaskType.Question => VerifyQuestion(),
                TaskType.QRCode => await ScanAndVerifyQRAsync(),
                TaskType.Action => await ConfirmActionAsync(),
                TaskType.SingleChoice => VerifySingleChoice(),
                TaskType.MultipleChoice => VerifyMultipleChoice(),
                _ => false
            };

            if (result)
                await HandleSuccessAsync();
            else
                HandleFailure();
        }
        finally
        {
            IsVerifying = false;
        }
    }

    //  Weryfikacja Question 

    private bool VerifyQuestion()
    {
        if (CurrentTask is null) return false;
        return _taskService.VerifyAnswer(CurrentTask, UserInput);
    }

    //  Weryfikacja QRCode 

    private async Task<bool> ScanAndVerifyQRAsync()
    {
        if (CurrentTask is null) return false;

        var scanned = await _scannerService.ScanAsync();
        if (scanned is null) return false;

        return _taskService.VerifyAnswer(CurrentTask, scanned);
    }

    //  Weryfikacja SingleChoice 

    private bool VerifySingleChoice()
    {
        if (CurrentTask is null) return false;
        return _taskService.VerifyAnswer(CurrentTask, SelectedOption);
    }

    //  Weryfikacja MultipleChoice 

    private bool VerifyMultipleChoice()
    {
        if (CurrentTask is null) return false;
        return _taskService.VerifyMultipleChoice(
            CurrentTask,
            GetSelectedMultipleOptions());
    }

    //  Potwierdzenie Action 

    private async Task<bool> ConfirmActionAsync()
    {
        return await Shell.Current.DisplayAlertAsync(
            "Potwierdzenie",
            "Czy wykonałeś/aś wymaganą czynność?",
            "Tak, wykonałem",
            "Jeszcze nie");
    }

    //  Obsługa wyniku 

    private async Task HandleSuccessAsync()
    {
        if (CurrentTask is null) return;

        IsCompleted = true;
        ResultMessage = "Poprawnie! Zadanie zaliczone.";

        var result = CurrentTask.Type switch
        {
            TaskType.Question => UserInput,
            TaskType.SingleChoice => SelectedOption,
            TaskType.MultipleChoice => string.Join(",",
                                           GetSelectedMultipleOptions()),
            _ => string.Empty
        };

        await _taskService.SaveCompletionAsync(new TaskCompletion
        {
            TaskId = CurrentTask.Id,
            CompletedAt = DateTime.Now,
            Result = result,
            IsCorrect = true
        });

        // Przelicz ile zadań ukończono
        var completions = await _taskService
            .GetCompletionsForSetAsync(AllTasks.First().LocationSetId);

        CompletedTasksCount = AllTasks
            .Count(t => completions.Any(c => c.TaskId == t.Id && c.IsCorrect));

        OnPropertyChanged(nameof(CanSubmitSet));
        OnPropertyChanged(nameof(SetProgress));

        if (HasNextTask)
        {
            await Task.Delay(1500);
            await NextTaskAsync();
        }
        else
        {
            ResultMessage = $"Ukończono {CompletedTasksCount}/{AllTasks.Count} zadań.";
        }
    }

    private void HandleFailure()
    {
        IsError = true;
        ResultMessage = CurrentTask?.Type switch
        {
            TaskType.Question => "Niepoprawna odpowiedź. Spróbuj jeszcze raz.",
            TaskType.QRCode => "Zeskanowany kod nie pasuje. Spróbuj ponownie.",
            TaskType.SingleChoice => "Niepoprawna odpowiedź. Spróbuj jeszcze raz.",
            TaskType.MultipleChoice => "Nie wszystkie odpowiedzi są poprawne. Spróbuj jeszcze raz.",
            _ => "Nie udało się. Spróbuj ponownie."
        };
    }
    // ── Zatwierdzenie zestawu ─────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmitSet))]
    private int _completedTasksCount;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private bool _isSetApproved;

    [ObservableProperty]
    private string _submitStatusMessage = string.Empty;

    // Przycisk "Zatwierdź zestaw" widoczny gdy wszystkie zadania zaliczone
    public bool CanSubmitSet =>
        AllTasks.Count > 0
        && CompletedTasksCount == AllTasks.Count
        && !IsSetApproved
        && !IsSubmitting;

    [RelayCommand]
    private async Task SubmitSetAsync()
    {
        IsSubmitting = true;
        SubmitStatusMessage = "Wysyłanie do weryfikacji...";

        await Task.Delay(500);
        SubmitStatusMessage = "Sprawdzanie przez administratora...";

        await Task.Delay(2500);
        SubmitStatusMessage = "Weryfikacja odpowiedzi...";

        await Task.Delay(2000);

        // Po 5s łącznego opóźnienia — zatwierdź
        IsSubmitting = false;
        IsSetApproved = true;
        SubmitStatusMessage = "Zestaw zatwierdzony! Gratulacje!";

        SetSubmitted?.Invoke();

        OnPropertyChanged(nameof(CanSubmitSet));
    }
    // sprawdzanie progresu
    public double SetProgress =>
        AllTasks.Count == 0
            ? 0.0
            : Math.Clamp((double)CompletedTasksCount / AllTasks.Count, 0.0, 1.0);

    [RelayCommand]
    private void SelectOption(string option)
    {
        SelectedOption = option;
    }
    public event Action? SetSubmitted;
}
