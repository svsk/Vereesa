using System.Collections.Generic;

namespace Vereesa.Data.Interfaces 
{
    public interface IRepository<T> where T : IEntity
    {
        IEnumerable<T> GetAll();
        T Add(T entity);
        void AddOrEdit(T entity);
        void Save();
        void Delete(T entity);
        T FindById(string id);
    }
}