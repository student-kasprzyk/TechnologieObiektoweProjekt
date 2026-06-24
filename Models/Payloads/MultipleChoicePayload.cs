namespace GraTerenowa.Models.Payloads;

public class MultipleChoicePayload
{
    public string Question { get; set; } = string.Empty;

    // Cztery opcje A/B/C/D
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;

    // Poprawne odpowiedzi np. ["A", "C"]
    public List<string> CorrectOptions { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public List<ChoiceOption> Options => new()
    {
        new("A", OptionA),
        new("B", OptionB),
        new("C", OptionC),
        new("D", OptionD)
    };
}