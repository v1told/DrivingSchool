using System.IO;
using DrivingSchool.Core.Data;

namespace DrivingSchool.App.Infrastructure;

/// <summary>
/// Фабрика контекстів бази даних. Для кожної операції створюється
/// новий короткоживучий контекст (рекомендований підхід для настільних застосунків).
/// </summary>
public static class Db
{
    public static string FilePath { get; private set; } =
        Path.Combine(AppContext.BaseDirectory, "drivingschool.db");

    public static void Initialize()
    {
        using var db = Create();
        DbInitializer.Initialize(db, DateTime.Now);
    }

    public static AppDbContext Create() => AppDbContext.CreateForFile(FilePath);
}
