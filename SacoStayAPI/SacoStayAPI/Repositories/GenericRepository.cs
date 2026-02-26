using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using System.Linq.Expressions;

namespace SacoStayAPI.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        //file này dùng để giao tiếp với db
        protected readonly ApplicationDBContext _context;
        protected readonly DbSet<T> _dbSet;
        //HÀM KHỞI TẠO DBContext
        public GenericRepository(ApplicationDBContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
 

        public async Task<T> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }
    }
}
