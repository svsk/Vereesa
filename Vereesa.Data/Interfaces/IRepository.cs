using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Vereesa.Data.Interfaces 
{
    public interface IRepository<T> where T : IEntity
    {
        IEnumerable<T> GetAll();
        T Add(T entity);
        //void AddMany(IEnumerable<T> entities);
        //IQueryable<T> FindBy(Expression<Func<T, bool>> predicate);
        void AddOrEdit(T entity);
        void Save();
    }
}