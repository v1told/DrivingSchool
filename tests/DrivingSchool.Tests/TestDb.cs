using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Tests;

/// <summary>
/// Тестова база даних SQLite у пам'яті з мінімальним набором ресурсів.
/// Кожен тест отримує власний екземпляр, тому тести незалежні.
/// </summary>
public sealed class TestDb : IDisposable
{
    /// <summary>Понеділок тестового тижня.</summary>
    public static readonly DateTime Monday = new(2026, 3, 2);

    private readonly SqliteConnection _connection;

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Context = CreateContext();
        Context.Database.EnsureCreated();
        Seed();
    }

    public AppDbContext Context { get; }

    public Vehicle ManualCar { get; private set; } = null!;
    public Vehicle AutoCar { get; private set; } = null!;
    public Instructor Theory { get; private set; } = null!;
    public Instructor Driver1 { get; private set; } = null!;
    public Instructor Driver2 { get; private set; } = null!;
    public StudyGroup Group { get; private set; } = null!;
    public Student Anna { get; private set; } = null!;
    public Student Ivan { get; private set; } = null!;

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        return new AppDbContext(options);
    }

    private void Seed()
    {
        ManualCar = new Vehicle { Model = "Skoda Fabia", PlateNumber = "ВК1111АА", Transmission = Transmission.Manual };
        AutoCar = new Vehicle { Model = "Toyota Yaris", PlateNumber = "ВК2222АА", Transmission = Transmission.Automatic };
        Context.Vehicles.AddRange(ManualCar, AutoCar);
        Context.SaveChanges();

        Theory = new Instructor { LastName = "Коваль", FirstName = "Олена", Phone = "+380671112233", TeachesTheory = true, TeachesDriving = false };
        Driver1 = new Instructor { LastName = "Бондар", FirstName = "Сергій", Phone = "+380671112234", TeachesDriving = true, VehicleId = ManualCar.Id };
        Driver2 = new Instructor { LastName = "Ткачук", FirstName = "Ігор", Phone = "+380671112235", TeachesDriving = true, VehicleId = AutoCar.Id };
        Context.Instructors.AddRange(Theory, Driver1, Driver2);

        Group = new StudyGroup { Name = "В-01", StartDate = Monday.AddDays(-14), EndDate = Monday.AddDays(60), Classroom = "Ауд. 1" };
        Context.Groups.Add(Group);
        Context.SaveChanges();

        Anna = new Student { LastName = "Шевченко", FirstName = "Анна", Phone = "+380501112233", BirthDate = new DateTime(2003, 5, 1), GroupId = Group.Id, RequiredDrivingHours = 4 };
        Ivan = new Student { LastName = "Бойко", FirstName = "Іван", Phone = "+380501112234", BirthDate = new DateTime(2002, 7, 9), GroupId = Group.Id, PreferredTransmission = Transmission.Automatic, RequiredDrivingHours = 4 };
        Context.Students.AddRange(Anna, Ivan);
        Context.SaveChanges();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
