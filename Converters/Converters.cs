using System.Globalization;
using GraTerenowa.Models;

namespace GraTerenowa.Converters;

// "Nowe zadanie" / "Edytuj zadanie" zależnie od bool
public class BoolToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = string.Empty;
    public string FalseValue { get; set; } = string.Empty;

    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true ? TrueValue : FalseValue;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// null - false, cokolwiek innego - true (podgląd QR)
public class NotNullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is not null;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// pusty string -> false, niepusty -> true (przycisk Eksportuj)
public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is string s && !string.IsNullOrWhiteSpace(s);

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// true -> false, false -> true (blokowanie pola po zaliczeniu)
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is not true;

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
// Liczba ukończonych / liczba wszystkich - wartość 0.0–1.0 dla ProgressBar
public class CountToProgressConverter : IValueConverter
{
    // Maksymalna liczba zadań przekazywana jako ConverterParameter
    public object Convert(object? value, Type t, object? parameter, CultureInfo c)
    {
        if (value is not int completed) return 0.0;
        if (parameter is not string s || !int.TryParse(s, out int total))
            return 0.0;

        return total == 0 ? 0.0 : Math.Clamp((double)completed / total, 0.0, 1.0);
    }

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
// Kolor przycisku opcji: zaznaczony = ciemnoniebieski, niezaznaczony = szary
public class OptionSelectedConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? parameter, CultureInfo c)
    {
        var selected = value as string;
        var option = parameter as string;
        return string.Equals(selected, option, StringComparison.OrdinalIgnoreCase)
            ? Color.FromArgb("#1565C0")
            : Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// TaskType - tekst przycisku wykonania
public class TaskTypeToButtonTextConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is TaskType type
            ? type switch
            {
                TaskType.Question => "Sprawdź odpowiedź",
                TaskType.QRCode => "Skanuj kod QR",
                TaskType.Action => "Potwierdzam wykonanie",
                TaskType.SingleChoice => "Zatwierdź wybór",
                TaskType.MultipleChoice => "Zatwierdź wybór",
                _ => "Wykonaj"
            }
            : "Wykonaj";

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}

// bool IsError - kolor tła komunikatu (zielony/czerwony)
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true
            ? Color.FromArgb("#C62828")   // czerwony = błąd
            : Color.FromArgb("#2E7D32");  // zielony = sukces

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}
// Zielony gdy aktywny, szary gdy nie
public class BoolToActiveColorConverter : IValueConverter
{
    public object Convert(object? value, Type t, object? p, CultureInfo c)
        => value is true
            ? Color.FromArgb("#4CAF50")
            : Color.FromArgb("#1E88E5");

    public object ConvertBack(object? value, Type t, object? p, CultureInfo c)
        => throw new NotImplementedException();
}