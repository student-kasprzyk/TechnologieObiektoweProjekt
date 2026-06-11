using SQLite;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GraTerenowa.Models;

[Table("LocationSets")]
public class LocationSet : INotifyPropertyChanged
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Właściwości obliczane (nie zapisywane do bazy)
    [Ignore]
    public int LocationCount { get; set; }

    private bool _isActive;

    [Ignore]
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}