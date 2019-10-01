using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LiteDB;
using Vereesa.Data.Interfaces;

namespace Vereesa.Data.Repositories
{
    public class LiteDBRepository<T> : IRepository<T> where T : IEntity
    {
        private const string DatabaseName = "VereesaData.db";
        private string _entityName;

        public LiteDBRepository()
        {
            _entityName = typeof(T).Name.ToLowerInvariant();
        }

        public T Add(T entity)
        {
            using (var db = new LiteDatabase(DatabaseName))
            {
                var collection = db.GetCollection<T>(_entityName);
                collection.Insert(entity);
                return entity;
            }
        }

        //Implement if needed
        // public void AddMany(IEnumerable<T> entities)
        // {
        //     using (var db = new LiteDatabase(DatabaseName))
        //     {
        //         var collection = db.GetCollection<T>(_entityName);
        //         collection.InsertBulk(entities);
        //     }
        // }

        public void AddOrEdit(T entity)
        {
            var existingEntities = FindBy(e => e.Id == entity.Id);

            if (existingEntities.Any())
            {
                Update(entity);
            }
            else
            {
                Add(entity);
            }
        }

        public IQueryable<T> FindBy(Expression<Func<T, bool>> predicate)
        {
            using (var db = new LiteDatabase(DatabaseName))
            {
                var collection = db.GetCollection<T>(_entityName);
                return collection.Find(predicate).AsQueryable();
            }
        }

        public IEnumerable<T> GetAll()
        {
            return ListAll();
        }

        private IEnumerable<T> ListAll()
        {
            using (var db = new LiteDatabase(DatabaseName))
            {
                var collection = db.GetCollection<T>(_entityName);
                return collection.FindAll();
            }
        }

        public void Save()
        {
            //Pointless in this context. 
        }

        public void Update(T entity)
        {
            using (var db = new LiteDatabase(DatabaseName))
            {
                var collection = db.GetCollection<T>(_entityName);
                collection.Update(entity);
            }
        }

        public void Delete(T entity)
        {
            using (var db = new LiteDatabase(DatabaseName)) 
            {
                var collection = db.GetCollection<T>(_entityName);
                collection.Delete(entity.Id);
            }
        }

        public T FindById(string id)
        {
            throw new NotImplementedException();
        }

        public Task<T> FindByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<T> AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task AddOrEditAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task SaveAsync()
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T entity)
        {
            throw new NotImplementedException();
        }
    }
}