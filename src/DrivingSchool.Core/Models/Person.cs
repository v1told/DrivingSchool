using DrivingSchool.Core.Common;

namespace DrivingSchool.Core.Models;

/// <summary>
/// Абстрактний базовий клас для учасників навчального процесу.
/// Містить спільні дані та абстрактну властивість RoleName,
/// яку кожен нащадок перевизначає по-своєму (поліморфізм).
/// </summary>
public abstract class Person : IEntity
{
    public int Id { get; set; }

    private string _lastName = string.Empty;
    private string _firstName = string.Empty;

    public string LastName
    {
        get => _lastName;
        set => _lastName = Normalize(value);
    }

    public string FirstName
    {
        get => _firstName;
        set => _firstName = Normalize(value);
    }

    public string? MiddleName { get; set; }

    public string Phone { get; set; } = string.Empty;

    /// <summary>Повне ім'я у форматі «Прізвище Ім'я По батькові».</summary>
    public string FullName =>
        string.IsNullOrWhiteSpace(MiddleName)
            ? $"{LastName} {FirstName}"
            : $"{LastName} {FirstName} {MiddleName}";

    /// <summary>Скорочене ім'я у форматі «Прізвище І.».</summary>
    public string ShortName =>
        FirstName.Length > 0 ? $"{LastName} {FirstName[0]}." : LastName;

    /// <summary>Роль особи в автошколі; визначається в класах-нащадках.</summary>
    public abstract string RoleName { get; }

    public override string ToString() => $"{RoleName}: {FullName}";

    /// <summary>
    /// Інкапсуляція: значення нормалізується при присвоєнні
    /// (прибираються зайві пробіли, перша літера стає великою).
    /// </summary>
    private static string Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? trimmed : char.ToUpper(trimmed[0]) + trimmed[1..];
    }
}
