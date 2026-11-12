namespace DrivingSchool.Core.Common;

/// <summary>
/// Виняток предметної області: порушення бізнес-правил автошколи
/// (конфлікт у розкладі, недопустимий запис на заняття тощо).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
