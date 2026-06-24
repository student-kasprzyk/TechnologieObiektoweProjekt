using SQLite;

namespace GraTerenowa.Models;

public enum TaskType
{
    Question = 0,  // pytanie otwarte
    QRCode = 1,  // skanowanie kodu QR
    Action = 2,  // czynność do potwierdzenia
    SingleChoice = 3,  // pytanie A/B/C/D (jedna odpowiedź)
    MultipleChoice = 4   // pytanie wielokrotnego wyboru
}

[Table("LocationTasks")]
public class LocationTask
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int LocationSetId { get; set; }

    public TaskType Type { get; set; }

    public string Payload { get; set; } = "{}";

    public string Title { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string TypeDisplayName => Type switch
    {
        TaskType.Question => "Pytanie otwarte",
        TaskType.QRCode => "Kod QR",
        TaskType.Action => "Czynność",
        TaskType.SingleChoice => "Pytanie A/B/C/D",
        TaskType.MultipleChoice => "Wielokrotny wybór",
        _ => string.Empty
    };
}