using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vereesa.Neon.Data.Interfaces;

namespace Vereesa.Neon.Tests.TestResources;

public class InMemoryRepository<T> : IRepository<T>
    where T : IEntity
{
    private List<T> _data = new List<T>();

    public T Add(T entity)
    {
        _data.Add(entity);
        return entity;
    }

    public Task<T> AddAsync(T entity)
    {
        _data.Add(entity);
        return Task.FromResult(entity);
    }

    public void AddOrEdit(T entity)
    {
        _data.Add(entity);
    }

    public Task AddOrEditAsync(T entity)
    {
        _data.Add(entity);
        return Task.CompletedTask;
    }

    public void Delete(T entity)
    {
        _data = _data.Where(x => x.Id != entity.Id).ToList();
    }

    public Task DeleteAsync(T entity)
    {
        _data = _data.Where(x => x.Id != entity.Id).ToList();
        return Task.CompletedTask;
    }

    public T FindById(string id)
    {
        return _data.FirstOrDefault(x => x.Id == id);
    }

    public Task<T> FindByIdAsync(string id)
    {
        return Task.FromResult(_data.FirstOrDefault(x => x.Id == id));
    }

    public IEnumerable<T> GetAll()
    {
        return new List<T>(_data);
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<T>>(new List<T>(_data));
    }

    public Task<bool> ItemExistsAsync(string id)
    {
        return Task.FromResult(_data.Any(x => x.Id == id));
    }

    public void Save()
    {
        return;
    }

    public Task SaveAsync()
    {
        return Task.CompletedTask;
    }
}
