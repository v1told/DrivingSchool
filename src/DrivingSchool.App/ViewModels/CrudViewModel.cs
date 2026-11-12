using System.Collections.ObjectModel;
using DrivingSchool.App.Infrastructure;
using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.App.ViewModels;

/// <summary>
/// Узагальнена модель представлення для довідників (патерн «Шаблонний метод»).
/// Загальний алгоритм роботи зі списком і формою реалізований тут,
/// а конкретні кроки (завантаження, копіювання, збереження) визначають нащадки.
/// </summary>
public abstract class CrudViewModel<T> : ViewModelBase where T : class, IEntity
{
    private T? _selected;
    private T _edit;

    protected CrudViewModel()
    {
        _edit = CreateNew();
        NewCommand = new RelayCommand(New);
        SaveCommand = new RelayCommand(Save);
        DeleteCommand = new RelayCommand(Delete, () => Selected != null);
    }

    public ObservableCollection<T> Items { get; } = new();

    public RelayCommand NewCommand { get; }
    public RelayCommand SaveCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public T? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value) && value != null)
            {
                Edit = Clone(value);
            }
        }
    }

    /// <summary>Копія запису, з якою працює форма редагування.</summary>
    public T Edit
    {
        get => _edit;
        set => SetProperty(ref _edit, value);
    }

    protected abstract string EntityName { get; }

    protected abstract IEnumerable<T> LoadItems(ResourceService service);

    protected abstract T CreateNew();

    protected abstract T Clone(T source);

    protected abstract void SaveItem(ResourceService service, T item);

    protected virtual string Describe(T item) => item.ToString() ?? EntityName;

    /// <summary>Додаткове завантаження довідників для форми (групи, автомобілі тощо).</summary>
    protected virtual void LoadLookups(ResourceService service)
    {
    }

    public void Load()
    {
        // Знімок форми: при перезавантаженні довідників WPF може скинути
        // прив'язані значення списків, тому після оновлення форму відновлюємо.
        var snapshot = Clone(Edit);
        using AppDbContext db = Db.Create();
        var service = new ResourceService(db);
        LoadLookups(service);
        Items.Clear();
        foreach (var item in LoadItems(service))
        {
            Items.Add(item);
        }
        Edit = snapshot;
        Status = $"Записів: {Items.Count}";
    }

    /// <summary>Початкове завантаження: список записів і порожня форма.</summary>
    protected void Initialize()
    {
        Load();
        New();
    }

    protected void New()
    {
        Selected = null;
        Edit = CreateNew();
    }

    private void Save()
    {
        var item = Clone(Edit);
        if (Run(() =>
            {
                using var db = Db.Create();
                SaveItem(new ResourceService(db), item);
            }, $"{EntityName}: зміни збережено."))
        {
            Load();
            Selected = Items.FirstOrDefault(i => i.Id == item.Id);
        }
    }

    private void Delete()
    {
        if (Selected == null || !Confirm($"Видалити запис «{Describe(Selected)}»?")) return;
        int id = Selected.Id;
        if (Run(() =>
            {
                using var db = Db.Create();
                new ResourceService(db).Delete<T>(id);
            }, $"{EntityName}: запис видалено."))
        {
            Load();
            New();
        }
    }
}
