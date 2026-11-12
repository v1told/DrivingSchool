using System.Text.RegularExpressions;
using DrivingSchool.Core.Common;
using DrivingSchool.Core.Data;
using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Scheduling;

/// <summary>
/// Облік ресурсів автошколи: учнів, інструкторів, автомобілів і груп.
/// Перед збереженням кожен об'єкт проходить перевірку коректності даних.
/// </summary>
public class ResourceService
{
    private static readonly Regex PhoneRegex = new(@"^\+?\d{10,12}$");
    private static readonly Regex PlateRegex = new(@"^[A-ZА-ЯІЇЄ]{2}\d{4}[A-ZА-ЯІЇЄ]{2}$");

    private readonly AppDbContext _db;

    public ResourceService(AppDbContext db)
    {
        _db = db;
    }

    // ---------- Учні ----------

    public List<Student> GetStudents(string? search = null, int? groupId = null)
    {
        var query = _db.Students.Include(s => s.Group).AsNoTracking().AsQueryable();
        if (groupId.HasValue)
        {
            query = query.Where(s => s.GroupId == groupId.Value);
        }
        var list = query.ToList();
        if (!string.IsNullOrWhiteSpace(search))
        {
            list = list.Where(s => s.FullName.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase)
                                   || s.Phone.Contains(search.Trim()))
                       .ToList();
        }
        return list.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
    }

    public void SaveStudent(Student student)
    {
        ValidateName(student);
        ValidatePhone(student.Phone);
        if (student.BirthDate > DateTime.Today.AddYears(-16))
        {
            throw new DomainException("Вік учня має бути не менше 16 років.");
        }
        if (student.RequiredDrivingHours <= 0 || student.RequiredDrivingHours > 200)
        {
            throw new DomainException("Кількість годин водіння має бути в межах 1–200.");
        }
        if (student.GroupId.HasValue)
        {
            var group = _db.Groups.Find(student.GroupId.Value)
                ?? throw new DomainException("Обрану групу не знайдено.");
            if (group.Category != student.Category)
            {
                throw new DomainException($"Група {group.Name} навчає за категорією {group.Category}, а учень за категорією {student.Category}.");
            }
        }
        student.Group = null; // зберігаємо лише зовнішній ключ, без графа об'єктів
        Upsert(student);
    }

    // ---------- Інструктори ----------

    public List<Instructor> GetInstructors() =>
        _db.Instructors.Include(i => i.Vehicle).AsNoTracking()
            .OrderBy(i => i.LastName).ToList();

    public void SaveInstructor(Instructor instructor)
    {
        ValidateName(instructor);
        ValidatePhone(instructor.Phone);
        if (!instructor.TeachesTheory && !instructor.TeachesDriving)
        {
            throw new DomainException("Оберіть хоча б один вид занять, які проводить викладач.");
        }
        if (instructor.VehicleId.HasValue)
        {
            bool taken = _db.Instructors.Any(i => i.VehicleId == instructor.VehicleId && i.Id != instructor.Id);
            if (taken)
            {
                throw new DomainException("Цей автомобіль уже закріплений за іншим інструктором.");
            }
        }
        instructor.Vehicle = null;
        Upsert(instructor);
    }

    // ---------- Автомобілі ----------

    public List<Vehicle> GetVehicles() =>
        _db.Vehicles.AsNoTracking().OrderBy(v => v.Model).ToList();

    public void SaveVehicle(Vehicle vehicle)
    {
        vehicle.PlateNumber = (vehicle.PlateNumber ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(vehicle.Model))
        {
            throw new DomainException("Вкажіть марку та модель автомобіля.");
        }
        if (!PlateRegex.IsMatch(vehicle.PlateNumber))
        {
            throw new DomainException("Номерний знак має формат АА1234ВВ.");
        }
        if (_db.Vehicles.Any(v => v.PlateNumber == vehicle.PlateNumber && v.Id != vehicle.Id))
        {
            throw new DomainException("Автомобіль з таким номером уже існує.");
        }
        Upsert(vehicle);
    }

    // ---------- Групи ----------

    public List<StudyGroup> GetGroups() =>
        _db.Groups.Include(g => g.Students).AsNoTracking()
            .OrderBy(g => g.StartDate).ToList();

    public void SaveGroup(StudyGroup group)
    {
        if (string.IsNullOrWhiteSpace(group.Name))
        {
            throw new DomainException("Вкажіть назву групи.");
        }
        if (group.EndDate <= group.StartDate)
        {
            throw new DomainException("Дата завершення навчання має бути пізнішою за дату початку.");
        }
        if (_db.Groups.Any(g => g.Name == group.Name && g.Id != group.Id))
        {
            throw new DomainException("Група з такою назвою вже існує.");
        }
        group.Students = new List<Student>();
        Upsert(group);
    }

    // ---------- Видалення ----------

    public void Delete<T>(int id) where T : class, IEntity
    {
        if (typeof(T) == typeof(Instructor) && _db.Lessons.Any(l => l.InstructorId == id))
        {
            throw new DomainException("Неможливо видалити викладача, у якого є заняття в розкладі.");
        }
        if (typeof(T) == typeof(Vehicle) && _db.DrivingLessons.Any(l => l.VehicleId == id))
        {
            throw new DomainException("Неможливо видалити автомобіль, що використовується в розкладі.");
        }
        new Repository<T>(_db).Delete(id);
    }

    private void Upsert<T>(T entity) where T : class, IEntity
    {
        var repository = new Repository<T>(_db);
        if (entity.Id == 0)
        {
            repository.Add(entity);
        }
        else
        {
            repository.Update(entity);
        }
    }

    private static void ValidateName(Person person)
    {
        if (string.IsNullOrWhiteSpace(person.LastName) || string.IsNullOrWhiteSpace(person.FirstName))
        {
            throw new DomainException("Вкажіть прізвище та ім'я.");
        }
    }

    private static void ValidatePhone(string phone)
    {
        var digits = (phone ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        if (!PhoneRegex.IsMatch(digits))
        {
            throw new DomainException("Телефон має містити 10–12 цифр, наприклад +380501234567.");
        }
    }
}
