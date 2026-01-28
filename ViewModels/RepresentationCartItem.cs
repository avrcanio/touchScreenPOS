using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TouchScreenPOS.ViewModels;

public sealed class RepresentationCartItem : INotifyPropertyChanged
{
    private decimal _quantity;
    private decimal _price;

    public int ArtiklId { get; init; }
    public string? Name { get; init; }
    public string? Image { get; init; }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (_quantity != value)
            {
                _quantity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Amount));
            }
        }
    }

    public decimal Price
    {
        get => _price;
        set
        {
            if (_price != value)
            {
                _price = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Amount));
            }
        }
    }

    public decimal Amount => Quantity * Price;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
