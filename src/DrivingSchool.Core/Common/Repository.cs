using DrivingSchool.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DrivingSchool.Core.Common;

/// <summary>
/// Узагальнений репозиторій для базових операцій CRUD над будь-якою сутністю.
/// Параметр типу обмежений інтерфейсом IEntity, тому репозиторій працює
/// лише з класами, що мають ідентифікатор.
/// </summary>
public class Repository<T> where T : class, IEntity
{
    private readonly AppDbContext _db;

    public Repository(AppDbContext db)
    {
        _db = db;
    }

    public List<T> GetAll() => _db.Set<T>().AsNoTracking().ToList();

    public T? GetById(int id) => _db.Set<T>().Find(id);

    public void Add(T entity)
    {
        _db.Set<T>().Add(entity);
        _db.SaveChanges();
    }

    public void Update(T entity)
    {
        _db.Set<T>().Update(entity);
        _db.SaveChanges();
    }

    public void Delete(int id)
    {
        var entity = _db.Set<T>().Find(id)
            ?? throw new DomainException("Запис для видалення не знайдено.");
        _db.Set<T>().Remove(entity);
        _db.SaveChanges();
    }
}
