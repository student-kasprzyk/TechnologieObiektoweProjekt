namespace GraTerenowa.Models.Payloads;

public class SingleChoicePayload
{
    public string Question { get; set; } = string.Empty;

    // Cztery opcje A/B/C/D
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;

    // Poprawna odpowiedź: "A", "B", "C" lub "D"
    public string CorrectOption { get; set; } = string.Empty;

    // Pomocnicza lista do wyświetlania w UI
    [System.Text.Json.Serialization.JsonIgnore]
    public List<ChoiceOption> Options => new()
    {
        new("A", OptionA),
        new("B", OptionB),
        new("C", OptionC),
        new("D", OptionD)
    };
}

public record ChoiceOption(string Key, string Text);