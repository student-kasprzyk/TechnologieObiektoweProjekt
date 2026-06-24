using System.Text.Json;
using GraTerenowa.Models;
using GraTerenowa.Models.Payloads;

namespace GraTerenowa.Services;

public class TaskService
{
    private readonly DatabaseService _db;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TaskService(DatabaseService db)
    {
        _db = db;
    }

    // Odczyt 

    public async Task<List<LocationTask>> GetBySetIdAsync(int locationSetId)
    {
        var db = await _db.GetDbAsync();
        return await db.Table<LocationTask>()
                       .Where(t => t.LocationSetId == locationSetId)
                       .OrderBy(t => t.SortOrder)
                       .ToListAsync();
    }

    public async Task<LocationTask?> GetByIdAsync(int id)
    {
        var db = await _db.GetDbAsync();
        return await db.Table<LocationTask>()
                       .Where(t => t.Id == id)
                       .FirstOrDefaultAsync();
    }

    // Zapis 

    public async Task<int> SaveAsync(LocationTask task)
    {
        var db = await _db.GetDbAsync();

        if (task.Id == 0)
            return await db.InsertAsync(task);

        await db.UpdateAsync(task);
        return task.Id;
    }

    public async Task SaveManyAsync(List<LocationTask> tasks)
    {
        var db = await _db.GetDbAsync();
        await db.InsertAllAsync(tasks);
    }

    //  Usuwanie 

    public async Task DeleteAsync(int id)
    {
        var db = await _db.GetDbAsync();

        await db.Table<TaskCompletion>()
                .Where(c => c.TaskId == id)
                .DeleteAsync();

        await db.DeleteAsync<LocationTask>(id);
    }
    // Nowe metody Payload 

    public SingleChoicePayload? GetSingleChoicePayload(LocationTask task)
        => JsonSerializer.Deserialize<SingleChoicePayload>(
               task.Payload, JsonOptions);

    public MultipleChoicePayload? GetMultipleChoicePayload(LocationTask task)
        => JsonSerializer.Deserialize<MultipleChoicePayload>(
               task.Payload, JsonOptions);

    public static string BuildSingleChoicePayload(
        string question,
        string optionA, string optionB,
        string optionC, string optionD,
        string correctOption)
        => JsonSerializer.Serialize(new SingleChoicePayload
        {
            Question = question,
            OptionA = optionA,
            OptionB = optionB,
            OptionC = optionC,
            OptionD = optionD,
            CorrectOption = correctOption.ToUpper()
        });

    public static string BuildMultipleChoicePayload(
        string question,
        string optionA, string optionB,
        string optionC, string optionD,
        List<string> correctOptions)
        => JsonSerializer.Serialize(new MultipleChoicePayload
        {
            Question = question,
            OptionA = optionA,
            OptionB = optionB,
            OptionC = optionC,
            OptionD = optionD,
            CorrectOptions = correctOptions
                                 .Select(o => o.ToUpper())
                                 .ToList()
        });


    //  Weryfikacja odpowiedzi 

    public bool VerifyAnswer(LocationTask task, string userInput)
    {
        return task.Type switch
        {
            TaskType.Question => VerifyQuestion(task.Payload, userInput),
            TaskType.QRCode => VerifyQRCode(task.Payload, userInput),
            TaskType.Action => true,
            TaskType.SingleChoice => VerifySingleChoice(task.Payload, userInput),
            TaskType.MultipleChoice => false, // wielokrotny wybór ma osobną metodę
            _ => false
        };
    }

    // Osobna metoda dla wielokrotnego wyboru — przyjmuje listę
    public bool VerifyMultipleChoice(LocationTask task, List<string> selected)
    {
        var data = GetMultipleChoicePayload(task);
        if (data is null) return false;

        var correct = data.CorrectOptions.OrderBy(x => x).ToList();
        var answered = selected.Select(s => s.ToUpper())
                               .OrderBy(x => x)
                               .ToList();

        return correct.SequenceEqual(answered);
    }

    private bool VerifySingleChoice(string payload, string selected)
    {
        var data = JsonSerializer.Deserialize<SingleChoicePayload>(
            payload, JsonOptions);

        return string.Equals(
            data?.CorrectOption,
            selected.Trim().ToUpper(),
            StringComparison.OrdinalIgnoreCase);
    }

    private bool VerifyQuestion(string payload, string userInput)
    {
        var data = JsonSerializer.Deserialize<QuestionPayload>(
            payload, JsonOptions);

        return string.Equals(
            data?.Answer.Trim(),
            userInput.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    private bool VerifyQRCode(string payload, string scannedCode)
    {
        var data = JsonSerializer.Deserialize<QRCodePayload>(
            payload, JsonOptions);

        return string.Equals(
            data?.ExpectedCode.Trim(),
            scannedCode.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    // Odczyt Payload 

    public QuestionPayload? GetQuestionPayload(LocationTask task)
        => JsonSerializer.Deserialize<QuestionPayload>(
               task.Payload, JsonOptions);

    public QRCodePayload? GetQRCodePayload(LocationTask task)
        => JsonSerializer.Deserialize<QRCodePayload>(
               task.Payload, JsonOptions);

    public ActionPayload? GetActionPayload(LocationTask task)
        => JsonSerializer.Deserialize<ActionPayload>(
               task.Payload, JsonOptions);

    //  Budowanie Payload 

    public static string BuildQuestionPayload(string question, string answer)
        => JsonSerializer.Serialize(
               new QuestionPayload { Question = question, Answer = answer });

    public static string BuildQRCodePayload(string expectedCode)
        => JsonSerializer.Serialize(
               new QRCodePayload { ExpectedCode = expectedCode });

    public static string BuildActionPayload(string description)
        => JsonSerializer.Serialize(
               new ActionPayload { Description = description });

    //  Ukończenia zadań 

    public async Task SaveCompletionAsync(TaskCompletion completion)
    {
        var db = await _db.GetDbAsync();
        await db.InsertAsync(completion);
    }

    public async Task<List<TaskCompletion>> GetCompletionsForSetAsync(
        int locationSetId)
    {
        var db = await _db.GetDbAsync();
        var tasks = await GetBySetIdAsync(locationSetId);
        var ids = tasks.Select(t => t.Id).ToHashSet();

        return await db.Table<TaskCompletion>()
                       .Where(c => ids.Contains(c.TaskId))
                       .ToListAsync();
    }

    public async Task<bool> IsTaskCompletedAsync(int taskId)
    {
        var db = await _db.GetDbAsync();
        return await db.Table<TaskCompletion>()
                       .Where(c => c.TaskId == taskId && c.IsCorrect)
                       .CountAsync() > 0;
    }
}