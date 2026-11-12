using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using DrivingSchool.Core.Common;

namespace DrivingSchool.App.Infrastructure;

/// <summary>
/// Базовий клас моделей представлення (MVVM). Реалізує INotifyPropertyChanged,
/// щоб інтерфейс автоматично оновлювався при зміні властивостей.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _status = string.Empty;

    /// <summary>Рядок стану для повідомлень користувачу.</summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Виконує дію з обробкою винятків: порушення бізнес-правил показуються
    /// як попередження, інші помилки як повідомлення про помилку.
    /// </summary>
    protected bool Run(Action action, string? successMessage = null)
    {
        try
        {
            action();
            if (successMessage != null)
            {
                Status = successMessage;
            }
            return true;
        }
        catch (DomainException ex)
        {
            Status = ex.Message.Split('\n')[0];
            MessageBox.Show(ex.Message, "Перевірка даних", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            Status = "Помилка: " + ex.Message;
            MessageBox.Show(ex.Message, "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        return false;
    }

    protected static bool Confirm(string question) =>
        MessageBox.Show(question, "Підтвердження", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
