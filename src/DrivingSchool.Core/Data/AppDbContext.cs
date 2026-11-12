using DrivingSchool.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Data;

/// <summary>
/// Контекст бази даних SQLite. Відображає класи предметної області
/// на таблиці за допомогою Entity Framework Core.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<StudyGroup> Groups => Set<StudyGroup>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<TheoryLesson> TheoryLessons => Set<TheoryLesson>();
    public DbSet<DrivingLesson> DrivingLessons => Set<DrivingLesson>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<AttendanceRecord> Attendance => Set<AttendanceRecord>();
    public DbSet<Grade> Grades => Set<Grade>();

    /// <summary>Створює контекст для файлу бази даних SQLite.</summary>
    public static AppDbContext CreateForFile(string dbPath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new AppDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ієрархія занять зберігається в одній таблиці (Table-per-Hierarchy)
        // зі стовпцем-дискримінатором LessonType.
        modelBuilder.Entity<Lesson>(e =>
        {
            e.ToTable("Lessons");
            e.HasDiscriminator<string>("LessonType")
                .HasValue<TheoryLesson>("Theory")
                .HasValue<DrivingLesson>("Driving");
            e.Property(l => l.Topic).HasMaxLength(200);
            e.HasOne(l => l.Instructor)
                .WithMany(i => i.Lessons)
                .HasForeignKey(l => l.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(l => l.Start);
        });

        modelBuilder.Entity<TheoryLesson>(e =>
        {
            e.HasOne(l => l.Group)
                .WithMany()
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(l => l.Classroom).HasMaxLength(50);
        });

        modelBuilder.Entity<DrivingLesson>(e =>
        {
            e.HasOne(l => l.Vehicle)
                .WithMany()
                .HasForeignKey(l => l.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Ignore(l => l.ActiveBooking);
            e.Ignore(l => l.IsFree);
        });

        modelBuilder.Entity<Student>(e =>
        {
            e.Property(s => s.LastName).HasMaxLength(60).IsRequired();
            e.Property(s => s.FirstName).HasMaxLength(60).IsRequired();
            e.Property(s => s.Phone).HasMaxLength(20);
            e.HasOne(s => s.Group)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Instructor>(e =>
        {
            e.Property(i => i.LastName).HasMaxLength(60).IsRequired();
            e.Property(i => i.FirstName).HasMaxLength(60).IsRequired();
            e.Property(i => i.Phone).HasMaxLength(20);
            e.HasOne(i => i.Vehicle)
                .WithMany()
                .HasForeignKey(i => i.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Vehicle>(e =>
        {
            e.Property(v => v.Model).HasMaxLength(60).IsRequired();
            e.Property(v => v.PlateNumber).HasMaxLength(10).IsRequired();
            e.HasIndex(v => v.PlateNumber).IsUnique();
        });

        modelBuilder.Entity<StudyGroup>(e =>
        {
            e.Property(g => g.Name).HasMaxLength(30).IsRequired();
            e.HasIndex(g => g.Name).IsUnique();
        });

        modelBuilder.Entity<Booking>(e =>
        {
            e.HasOne(b => b.Lesson)
                .WithMany(l => l.Bookings)
                .HasForeignKey(b => b.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Student)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AttendanceRecord>(e =>
        {
            e.HasOne(a => a.Lesson)
                .WithMany(l => l.Attendance)
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Student)
                .WithMany(s => s.Attendance)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => new { a.LessonId, a.StudentId }).IsUnique();
        });

        modelBuilder.Entity<Grade>(e =>
        {
            e.HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(g => g.Lesson)
                .WithMany()
                .HasForeignKey(g => g.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
