using System.Text.Json;
using GraTerenowa.Models;

namespace GraTerenowa.Services;

public class QuestionImportService
{
    private readonly TaskService _taskService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public QuestionImportService(TaskService taskService)
    {
        _taskService = taskService;
    }

    //Import pytań z JSON 

    public async Task ImportFromJsonAsync(
        int locationSetId,
        string jsonFileName = "questions.json")
    {
        using var stream = await FileSystem
            .OpenAppPackageFileAsync(jsonFileName);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var items = JsonSerializer.Deserialize<List<QuestionImportDto>>(
            json, JsonOptions);

        if (items is null || items.Count == 0) return;

        var tasks = items
            .Select((item, index) => new LocationTask
            {
                LocationSetId = locationSetId,
                Type = TaskType.Question,
                Title = item.Title,
                SortOrder = index + 1,
                Payload = TaskService.BuildQuestionPayload(
                                    item.Question, item.Answer)
            })
            .ToList();

        await _taskService.SaveManyAsync(tasks);
    }

    // Import pytań z CSV 
    // Format: Tytuł;Pytanie;Odpowiedź  (pierwsza linia = nagłówek)

    public async Task ImportFromCsvAsync(
        int locationSetId,
        string csvFileName = "questions.csv")
    {
        using var stream = await FileSystem
            .OpenAppPackageFileAsync(csvFileName);
        using var reader = new StreamReader(stream);

        await reader.ReadLineAsync(); // pomijamy nagłówek

        var tasks = new List<LocationTask>();
        int order = 1;

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(';');
            if (parts.Length < 3) continue;

            tasks.Add(new LocationTask
            {
                LocationSetId = locationSetId,
                Type = TaskType.Question,
                Title = parts[0].Trim(),
                SortOrder = order++,
                Payload = TaskService.BuildQuestionPayload(
                                    parts[1].Trim(),
                                    parts[2].Trim())
            });
        }

        if (tasks.Count > 0)
            await _taskService.SaveManyAsync(tasks);
    }

    // Import zadań QR z JSON 
    // Format: [ { "title": "...", "code": "..." } ]

    public async Task ImportQRTasksFromJsonAsync(
        int locationSetId,
        string jsonFileName = "qr_tasks.json")
    {
        using var stream = await FileSystem
            .OpenAppPackageFileAsync(jsonFileName);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var items = JsonSerializer.Deserialize<List<QRImportDto>>(
            json, JsonOptions);

        if (items is null || items.Count == 0) return;

        var tasks = items
            .Select((item, index) => new LocationTask
            {
                LocationSetId = locationSetId,
                Type = TaskType.QRCode,
                Title = item.Title,
                SortOrder = index + 1,
                Payload = TaskService.BuildQRCodePayload(item.Code)
            })
            .ToList();

        await _taskService.SaveManyAsync(tasks);
    }

    // Import czynności z JSON
    // Format: [ { "title": "...", "description": "..." } ]

    public async Task ImportActionTasksFromJsonAsync(
        int locationSetId,
        string jsonFileName = "action_tasks.json")
    {
        using var stream = await FileSystem
            .OpenAppPackageFileAsync(jsonFileName);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var items = JsonSerializer.Deserialize<List<ActionImportDto>>(
            json, JsonOptions);

        if (items is null || items.Count == 0) return;

        var tasks = items
            .Select((item, index) => new LocationTask
            {
                LocationSetId = locationSetId,
                Type = TaskType.Action,
                Title = item.Title,
                SortOrder = index + 1,
                Payload = TaskService.BuildActionPayload(item.Description)
            })
            .ToList();

        await _taskService.SaveManyAsync(tasks);
    }


    // DTO do deserializacji plików 

    private record QuestionImportDto(
        string Title,
        string Question,
        string Answer);

    private record QRImportDto(
        string Title,
        string Code);

    private record ActionImportDto(
        string Title,
        string Description);
}