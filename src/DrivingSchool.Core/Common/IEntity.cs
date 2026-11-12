namespace DrivingSchool.Core.Common;

/// <summary>
/// Спільний контракт для всіх сутностей, що зберігаються в базі даних.
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}
