using SQLite;

namespace GraTerenowa.Models;

[Table("TaskCompletions")]
public class TaskCompletion
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Klucz obcy > LocationTask.Id</summary>
    [Indexed]
    public int TaskId { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.Now;

    /// <summary>Odpowiedź użytkownika lub wynik skanu QR.</summary>
    public string Result { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
}