using DrivingSchool.Core.Common;
using DrivingSchool.Core.Learning;
using DrivingSchool.Core.Models;
using DrivingSchool.Core.Scheduling;

namespace DrivingSchool.Core.Data;

/// <summary>
/// Створює базу даних і наповнює її демонстраційними даними:
/// ресурсами автошколи, розкладом за кілька тижнів, записами,
/// відмітками відвідування та оцінками.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext db, DateTime now)
    {
        db.Database.EnsureCreated();
        if (db.Students.Any())
        {
            return;
        }

        var rnd = new Random(2025);
        var thisWeek = DateHelper.StartOfWeek(now);

        // 1. Автомобілі
        var fabia = new Vehicle { Model = "Skoda Fabia", PlateNumber = "ВК1023АМ", Transmission = Transmission.Manual };
        var logan = new Vehicle { Model = "Renault Logan", PlateNumber = "ВК4517ВС", Transmission = Transmission.Manual };
        var yaris = new Vehicle { Model = "Toyota Yaris", PlateNumber = "ВК7781АХ", Transmission = Transmission.Automatic };
        var i20 = new Vehicle { Model = "Hyundai i20", PlateNumber = "ВК3390КА", Transmission = Transmission.Manual, IsAvailable = false };
        db.Vehicles.AddRange(fabia, logan, yaris, i20);
        db.SaveChanges();

        // 2. Викладачі та інструктори
        db.Instructors.AddRange(
            new Instructor { LastName = "Коваль", FirstName = "Олена", MiddleName = "Петрівна", Phone = "+380671112233", TeachesTheory = true, TeachesDriving = false, ExperienceYears = 12 },
            new Instructor { LastName = "Мельник", FirstName = "Андрій", MiddleName = "Васильович", Phone = "+380502223344", TeachesTheory = true, TeachesDriving = true, ExperienceYears = 9, VehicleId = fabia.Id },
            new Instructor { LastName = "Бондар", FirstName = "Сергій", MiddleName = "Іванович", Phone = "+380633334455", TeachesDriving = true, ExperienceYears = 15, VehicleId = logan.Id },
            new Instructor { LastName = "Ткачук", FirstName = "Ігор", MiddleName = "Миколайович", Phone = "+380974445566", TeachesDriving = true, ExperienceYears = 6, VehicleId = yaris.Id });
        db.SaveChanges();

        // 3. Групи
        var g1 = new StudyGroup { Name = "В-01", Category = LicenseCategory.B, StartDate = thisWeek.AddDays(-21), EndDate = thisWeek.AddDays(70), Classroom = "Ауд. 1" };
        var g2 = new StudyGroup { Name = "В-02", Category = LicenseCategory.B, StartDate = thisWeek.AddDays(-7), EndDate = thisWeek.AddDays(84), Classroom = "Ауд. 2" };
        db.Groups.AddRange(g1, g2);
        db.SaveChanges();

        // 4. Учні
        string[,] names =
        {
            { "Шевченко", "Анна", "Олегівна" }, { "Ковальчук", "Марія", "Ігорівна" },
            { "Бойко", "Дмитро", "Сергійович" }, { "Кравчук", "Олександр", "Петрович" },
            { "Олійник", "Юлія", "Андріївна" }, { "Лисенко", "Максим", "Вікторович" },
            { "Савчук", "Катерина", "Романівна" }, { "Руденко", "Назар", "Олегович" },
            { "Мороз", "Вікторія", "Юріївна" }, { "Павленко", "Богдан", "Іванович" }
        };
        var students = new List<Student>();
        for (int i = 0; i < names.GetLength(0); i++)
        {
            students.Add(new Student
            {
                LastName = names[i, 0],
                FirstName = names[i, 1],
                MiddleName = names[i, 2],
                Phone = $"+38067{5550000 + i * 137:0000000}",
                BirthDate = new DateTime(2000 + i % 7, 1 + i, 10 + i),
                EnrollmentDate = i < 6 ? g1.StartDate : g2.StartDate,
                GroupId = i < 6 ? g1.Id : g2.Id,
                Category = LicenseCategory.B,
                PreferredTransmission = i is 4 or 8 ? Transmission.Automatic : Transmission.Manual,
                RequiredDrivingHours = i is 4 or 8 ? 38 : 40
            });
        }
        db.Students.AddRange(students);
        db.SaveChanges();

        // 5. Розклад, записи та журнал за 3 минулі, поточний і наступний тижні
        for (int w = -3; w <= 1; w++)
        {
            var weekStart = thisWeek.AddDays(7 * w);
            new ScheduleGenerator(db).GenerateWeek(weekStart);

            // Записуємо учнів так, ніби запис відбувався напередодні тижня.
            var booking = new BookingService(db, new LearningPolicy(), new FixedClock(weekStart.AddHours(-12)));
            foreach (var s in students.Where(s => s.GroupId == g1.Id || w >= -1))
            {
                var slots = booking.GetAvailableSlots(s.Id, weekStart, weekStart.AddDays(7));
                var usedDays = new HashSet<DateTime>();
                foreach (var slot in slots.OrderBy(_ => rnd.Next()))
                {
                    if (usedDays.Count >= 2) break;
                    if (usedDays.Contains(slot.Start.Date)) continue;
                    try
                    {
                        booking.Book(s.Id, slot.Id);
                        usedDays.Add(slot.Start.Date);
                    }
                    catch (DomainException)
                    {
                        // Слот міг стати недоступним, пробуємо наступний.
                    }
                }
            }

            // Відмічаємо відвідування для занять, що вже відбулися.
            var attendance = new AttendanceService(db, new FixedClock(now));
            foreach (var lesson in attendance.GetLessonsForJournal(weekStart, weekStart.AddDays(7)))
            {
                if (lesson.End > now) continue;
                var journal = attendance.GetJournal(lesson.Id);
                foreach (var entry in journal)
                {
                    int r = rnd.Next(100);
                    entry.Status = lesson is DrivingLesson
                        ? (r < 94 ? AttendanceStatus.Present : AttendanceStatus.Absent)
                        : (r < 82 ? AttendanceStatus.Present : r < 90 ? AttendanceStatus.Late : r < 95 ? AttendanceStatus.Excused : AttendanceStatus.Absent);
                }
                attendance.SaveJournal(lesson.Id, journal);

                foreach (var entry in journal.Where(e => e.Status == AttendanceStatus.Present))
                {
                    if (lesson is DrivingLesson && rnd.Next(100) < 60)
                    {
                        attendance.AddGrade(entry.StudentId, GradeType.DrivingSkill, rnd.Next(6, 13), lesson.Id);
                    }
                    else if (lesson is TheoryLesson && rnd.Next(100) < 35)
                    {
                        attendance.AddGrade(entry.StudentId, GradeType.TheoryTest, rnd.Next(6, 13), lesson.Id);
                    }
                }
            }
        }

        // 6. Внутрішній іспит з теорії для двох учнів першої групи
        var exam = new AttendanceService(db, new FixedClock(now));
        exam.AddGrade(students[0].Id, GradeType.InternalTheoryExam, 10, null, "Внутрішній іспит з теорії");
        exam.AddGrade(students[1].Id, GradeType.InternalTheoryExam, 9, null, "Внутрішній іспит з теорії");
    }
}
