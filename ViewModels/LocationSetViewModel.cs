using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraTerenowa.Models;
using GraTerenowa.Services;
using GraTerenowa.Views;


// using GraTerenowa.Views; 
using System.Collections.ObjectModel;

namespace GraTerenowa.ViewModels;

public partial class LocationSetViewModel : ObservableObject
{
    private readonly LocationSetService _locationSetService;
    private readonly TaskService _taskService;
    private readonly QuestionImportService _importService;

    public LocationSetViewModel(
        LocationSetService locationSetService,
        TaskService taskService,
        QuestionImportService importService)
    {
        _locationSetService = locationSetService;
        _taskService = taskService;
        _importService = importService;
    }

    [ObservableProperty]
    private ObservableCollection<LocationSet> _allSets = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredSets))]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActiveSet))]
    private LocationSet? _activeSet;

    public bool HasActiveSet => ActiveSet is not null;

    public IEnumerable<LocationSet> FilteredSets =>
        string.IsNullOrWhiteSpace(SearchQuery)
            ? AllSets
            : AllSets.Where(s =>
                s.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                (s.Description?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false));

    partial void OnSearchQueryChanged(string value) =>
        OnPropertyChanged(nameof(FilteredSets));

    public async Task LoadSetsAsync()
    {
        var sets = await _locationSetService.GetAllAsync();
        AllSets = new ObservableCollection<LocationSet>(sets);

        var activeId = Preferences.Get("ActiveSetId", -1);
        ActiveSet = AllSets.FirstOrDefault(s => s.Id == activeId);

        foreach (var set in AllSets)
            set.IsActive = set.Id == activeId;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        await LoadSetsAsync();
        IsRefreshing = false;
    }

    [RelayCommand]
    private void SelectSet(LocationSet set)
    {
        if (ActiveSet is not null)
            ActiveSet.IsActive = false;

        set.IsActive = true;
        ActiveSet = set;

        Preferences.Set("ActiveSetId", set.Id);
    }

    [RelayCommand]
    private async Task AddSetAsync()
    {
        string? name = await Shell.Current.DisplayPromptAsync(
            "Nowy zestaw",
            "Podaj nazwę zestawu:",
            "Utwórz",
            "Anuluj",
            maxLength: 60);

        if (string.IsNullOrWhiteSpace(name)) return;

        if (await _locationSetService.ExistsAsync(name))
        {
            // POPRAWKA: Dodane \ przed cudzysłowem
            await Shell.Current.DisplayAlertAsync(
                "Błąd",
                $"Zestaw o nazwie \"{name}\" już istnieje.",
                "OK");
            return;
        }

        var newSet = new LocationSet { Name = name };
        var id = await _locationSetService.SaveAsync(newSet);
        newSet.Id = id;

        AllSets.Add(newSet);
        OnPropertyChanged(nameof(FilteredSets));
    }

    [RelayCommand]
    private async Task EditSetAsync(LocationSet set)
    {
        string? name = await Shell.Current.DisplayPromptAsync(
            "Edytuj zestaw",
            "Nowa nazwa:",
            "Zapisz",
            "Anuluj",
            initialValue: set.Name,
            maxLength: 60);

        if (string.IsNullOrWhiteSpace(name)) return;

        set.Name = name;
        await _locationSetService.SaveAsync(set);
        OnPropertyChanged(nameof(FilteredSets));
    }

    [RelayCommand]
    private async Task DeleteSetAsync(LocationSet set)
    {
        // POPRAWKA: Dodane \ przed cudzysłowem
        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Usuń zestaw",
            $"Na pewno usunąć \"{set.Name}\"?\nUsunięte zostaną też wszystkie zadania.",
            "Usuń",
            "Anuluj");

        if (!confirm) return;

        await _locationSetService.DeleteAsync(set.Id);
        AllSets.Remove(set);

        if (ActiveSet?.Id == set.Id)
        {
            ActiveSet = null;
            Preferences.Remove("ActiveSetId");
        }

        OnPropertyChanged(nameof(FilteredSets));
    }

    [RelayCommand]
    private async Task StartGameAsync()
    {
        if (ActiveSet is null)
        {
            await Shell.Current.DisplayAlertAsync(
                "Brak zestawu",
                "Wybierz aktywny zestaw przed rozpoczęciem gry.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(TaskDetailPage),
            new Dictionary<string, object>
            {
                ["SetId"] = ActiveSet.Id,
                ["PinTaskId"] = string.Empty   // brak pinu = uruchomiono ręcznie
            });
    }

    [RelayCommand]
    private async Task GoToTaskEditorAsync()
    {
        if (ActiveSet is null) return;

        var tasks = await _taskService.GetBySetIdAsync(ActiveSet.Id);

        await Shell.Current.GoToAsync(
            "TaskEditorPage",
            new Dictionary<string, object>
            {
                ["SetId"] = ActiveSet.Id,
                ["SortOrder"] = tasks.Count + 1
            });
    }

    [RelayCommand]
    private async Task ImportQuestionsAsync(string fileName = "questions.json")
    {
        if (ActiveSet is null) return;

        await _importService.ImportFromJsonAsync(ActiveSet.Id, fileName);
        await LoadSetsAsync();

        await Shell.Current.DisplayAlertAsync(
            "Import zakończony",
            "Pytania zostały dodane do zestawu.",
            "OK");
    }
}