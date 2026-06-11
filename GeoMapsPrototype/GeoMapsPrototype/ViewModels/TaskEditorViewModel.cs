using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraTerenowa.Models;
using GraTerenowa.Models.Payloads;
using GraTerenowa.Services;

namespace GraTerenowa.ViewModels;

public partial class TaskEditorViewModel : ObservableObject
{
    private readonly TaskService _taskService;
    private readonly QRCodeGeneratorService _qrService;

    //  Kontekst 

    public int ActiveSetId { get; set; }
    public int NextSortOrder { get; set; } = 1;

    private int _editingTaskId = 0;
    public bool IsEditing => _editingTaskId != 0;

    //  Konstruktor

    public TaskEditorViewModel(
        TaskService taskService,
        QRCodeGeneratorService qrService)
    {
        _taskService = taskService;
        _qrService = qrService;
    }

    //  Typ zadania 

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsQuestion),
        nameof(IsQRCode),
        nameof(IsAction),
        nameof(IsSingleChoice),
        nameof(IsMultipleChoice))]
    private TaskType _selectedType = TaskType.Question;

    public bool IsQuestion => SelectedType == TaskType.Question;
    public bool IsQRCode => SelectedType == TaskType.QRCode;
    public bool IsAction => SelectedType == TaskType.Action;
    public bool IsSingleChoice => SelectedType == TaskType.SingleChoice;
    public bool IsMultipleChoice => SelectedType == TaskType.MultipleChoice;

    public List<TaskType> AvailableTypes { get; } =
        Enum.GetValues<TaskType>().ToList();

    // Wspólne pole 

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _title = string.Empty;

    //  Pola dla Question 

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _questionText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _correctAnswer = string.Empty;

    //  Pola dla QRCode 

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _qrContent = string.Empty;

    [ObservableProperty]
    private ImageSource? _qrPreview;

    partial void OnQrContentChanged(string value)
    {
        QrPreview = string.IsNullOrWhiteSpace(value)
            ? null
            : _qrService.GenerateImageSource(value);
    }

    //  Pola dla Action 

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _actionDescription = string.Empty;

    //  Pola dla SingleChoice i MultipleChoice 

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _choiceQuestion = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _optionA = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _optionB = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _optionC = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _optionD = string.Empty;

    // SingleChoice — jedna poprawna odpowiedź
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private string _correctOption = string.Empty;

    public List<string> AvailableOptions { get; } = ["A", "B", "C", "D"];

    // MultipleChoice — checkboxy
    private bool _isACorrect;
    public bool IsACorrect
    {
        get => _isACorrect;
        set
        {
            SetProperty(ref _isACorrect, value);
            SaveTaskCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _isBCorrect;
    public bool IsBCorrect
    {
        get => _isBCorrect;
        set
        {
            SetProperty(ref _isBCorrect, value);
            SaveTaskCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _isCCorrect;
    public bool IsCCorrect
    {
        get => _isCCorrect;
        set
        {
            SetProperty(ref _isCCorrect, value);
            SaveTaskCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _isDCorrect;
    public bool IsDCorrect
    {
        get => _isDCorrect;
        set
        {
            SetProperty(ref _isDCorrect, value);
            SaveTaskCommand.NotifyCanExecuteChanged();
        }
    }

    // Pomocnicza lista zaznaczonych opcji dla MultipleChoice
    private List<string> SelectedCorrectOptions =>
        new[]
        {
            IsACorrect ? "A" : null,
            IsBCorrect ? "B" : null,
            IsCCorrect ? "C" : null,
            IsDCorrect ? "D" : null
        }
        .Where(x => x is not null)
        .Select(x => x!)
        .ToList();

    //  Walidacja 

    private bool CanSaveTask() => SelectedType switch
    {
        TaskType.Question =>
               !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(QuestionText)
            && !string.IsNullOrWhiteSpace(CorrectAnswer),

        TaskType.QRCode =>
               !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(QrContent),

        TaskType.Action =>
               !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(ActionDescription),

        TaskType.SingleChoice =>
               !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(ChoiceQuestion)
            && !string.IsNullOrWhiteSpace(OptionA)
            && !string.IsNullOrWhiteSpace(OptionB)
            && !string.IsNullOrWhiteSpace(OptionC)
            && !string.IsNullOrWhiteSpace(OptionD)
            && !string.IsNullOrWhiteSpace(CorrectOption),

        TaskType.MultipleChoice =>
               !string.IsNullOrWhiteSpace(Title)
            && !string.IsNullOrWhiteSpace(ChoiceQuestion)
            && !string.IsNullOrWhiteSpace(OptionA)
            && !string.IsNullOrWhiteSpace(OptionB)
            && !string.IsNullOrWhiteSpace(OptionC)
            && !string.IsNullOrWhiteSpace(OptionD)
            && SelectedCorrectOptions.Count > 0,

        _ => false
    };

    //  Komendy 

    [RelayCommand]
    private void GenerateRandomCode()
    {
        QrContent = $"GRA_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    [RelayCommand(CanExecute = nameof(CanSaveTask))]
    private async Task SaveTaskAsync()
    {
        var payload = SelectedType switch
        {
            TaskType.Question => TaskService.BuildQuestionPayload(
                                     QuestionText, CorrectAnswer),

            TaskType.QRCode => TaskService.BuildQRCodePayload(QrContent),

            TaskType.Action => TaskService.BuildActionPayload(ActionDescription),

            TaskType.SingleChoice => TaskService.BuildSingleChoicePayload(
                                     ChoiceQuestion,
                                     OptionA, OptionB, OptionC, OptionD,
                                     CorrectOption),

            TaskType.MultipleChoice => TaskService.BuildMultipleChoicePayload(
                                     ChoiceQuestion,
                                     OptionA, OptionB, OptionC, OptionD,
                                     SelectedCorrectOptions),
            _ => "{}"
        };

        var task = new LocationTask
        {
            Id = _editingTaskId,
            LocationSetId = ActiveSetId,
            Type = SelectedType,
            Title = Title.Trim(),
            SortOrder = NextSortOrder,
            Payload = payload
        };

        await _taskService.SaveAsync(task);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ExportQRAsync()
    {
        if (string.IsNullOrWhiteSpace(QrContent)) return;

        var fileName = $"QR_{QrContent}.png";
        var path = Path.Combine(FileSystem.CacheDirectory, fileName);

        await _qrService.SavePngAsync(QrContent, path);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = $"Kod QR – {Title}",
            File = new ShareFile(path)
        });
    }

    [RelayCommand]
    private async Task DeleteTaskAsync()
    {
        if (!IsEditing) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Usuń zadanie",
            $"Na pewno usunąć \"{Title}\"?",
            "Usuń",
            "Anuluj");

        if (!confirm) return;

        await _taskService.DeleteAsync(_editingTaskId);
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    // Ładowanie zadania do edycji 

    public async Task LoadTaskAsync(int taskId)
    {
        var task = await _taskService.GetByIdAsync(taskId);
        if (task is null) return;

        _editingTaskId = task.Id;
        SelectedType = task.Type;
        Title = task.Title;
        NextSortOrder = task.SortOrder;

        switch (task.Type)
        {
            case TaskType.Question:
                var q = _taskService.GetQuestionPayload(task);
                QuestionText = q?.Question ?? string.Empty;
                CorrectAnswer = q?.Answer ?? string.Empty;
                break;

            case TaskType.QRCode:
                var qr = _taskService.GetQRCodePayload(task);
                QrContent = qr?.ExpectedCode ?? string.Empty;
                break;

            case TaskType.Action:
                var a = _taskService.GetActionPayload(task);
                ActionDescription = a?.Description ?? string.Empty;
                break;

            case TaskType.SingleChoice:
                var sc = _taskService.GetSingleChoicePayload(task);
                ChoiceQuestion = sc?.Question ?? string.Empty;
                OptionA = sc?.OptionA ?? string.Empty;
                OptionB = sc?.OptionB ?? string.Empty;
                OptionC = sc?.OptionC ?? string.Empty;
                OptionD = sc?.OptionD ?? string.Empty;
                CorrectOption = sc?.CorrectOption ?? string.Empty;
                break;

            case TaskType.MultipleChoice:
                var mc = _taskService.GetMultipleChoicePayload(task);
                ChoiceQuestion = mc?.Question ?? string.Empty;
                OptionA = mc?.OptionA ?? string.Empty;
                OptionB = mc?.OptionB ?? string.Empty;
                OptionC = mc?.OptionC ?? string.Empty;
                OptionD = mc?.OptionD ?? string.Empty;
                IsACorrect = mc?.CorrectOptions.Contains("A") ?? false;
                IsBCorrect = mc?.CorrectOptions.Contains("B") ?? false;
                IsCCorrect = mc?.CorrectOptions.Contains("C") ?? false;
                IsDCorrect = mc?.CorrectOptions.Contains("D") ?? false;
                break;
        }
    }

    // Reset formularza

    public void Reset(int setId, int nextOrder)
    {
        ActiveSetId = setId;
        NextSortOrder = nextOrder;
        _editingTaskId = 0;
        SelectedType = TaskType.Question;
        Title = string.Empty;
        QuestionText = string.Empty;
        CorrectAnswer = string.Empty;
        QrContent = string.Empty;
        QrPreview = null;
        ActionDescription = string.Empty;
        ChoiceQuestion = string.Empty;
        OptionA = string.Empty;
        OptionB = string.Empty;
        OptionC = string.Empty;
        OptionD = string.Empty;
        CorrectOption = string.Empty;
        IsACorrect = false;
        IsBCorrect = false;
        IsCCorrect = false;
        IsDCorrect = false;
    }
}