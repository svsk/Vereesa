using System.Collections.Generic;
using System.Threading.Tasks;

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

		Task<bool> ItemExistsAsync(string id);
		Task<IEnumerable<T>> GetAllAsync();
		Task<T> AddAsync(T entity);
		Task AddOrEditAsync(T entity);
		Task SaveAsync();
		Task DeleteAsync(T entity);
		Task<T> FindByIdAsync(string id);
	}
}