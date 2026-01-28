using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TouchScreenPOS.ViewModels;

public sealed class ArtiklDisplay : INotifyPropertyChanged
{
    private string? _name;
    private string? _image;
    private string? _categoryName;
    private int? _categoryId;
    private string? _leafCategoryName;
    private int _categorySortOrder;
    private int _leafSortOrder;

    public int RmId { get; init; }

    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }


    public string? Image
    {
        get => _image;
        set
        {
            if (_image != value)
            {
                _image = value;
                OnPropertyChanged();
            }
        }
    }

    public string? CategoryName
    {
        get => _categoryName;
        set
        {
            if (_categoryName != value)
            {
                _categoryName = value;
                OnPropertyChanged();
            }
        }
    }

    public int? CategoryId
    {
        get => _categoryId;
        set
        {
            if (_categoryId != value)
            {
                _categoryId = value;
                OnPropertyChanged();
            }
        }
    }

    public string? LeafCategoryName
    {
        get => _leafCategoryName;
        set
        {
            if (_leafCategoryName != value)
            {
                _leafCategoryName = value;
                OnPropertyChanged();
            }
        }
    }

    public int CategorySortOrder
    {
        get => _categorySortOrder;
        set
        {
            if (_categorySortOrder != value)
            {
                _categorySortOrder = value;
                OnPropertyChanged();
            }
        }
    }

    public int LeafSortOrder
    {
        get => _leafSortOrder;
        set
        {
            if (_leafSortOrder != value)
            {
                _leafSortOrder = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
