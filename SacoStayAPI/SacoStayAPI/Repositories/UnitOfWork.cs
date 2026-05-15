
using SacoStayAPI.Data;
using SacoStayAPI.Repositories;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace SacoStayAPI.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _context;
        private Hashtable _repositories;
        private LifestyleRepository _lifestyleRepository;

        public UnitOfWork(ApplicationDBContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            if (_repositories == null)
                _repositories = new Hashtable();

            var type = typeof(T).Name;

            if (!_repositories.ContainsKey(type))
            {
                var repositoryType = typeof(GenericRepository<>);
                var repositoryInstance = Activator.CreateInstance(repositoryType.MakeGenericType(typeof(T)), _context);
                _repositories.Add(type, repositoryInstance);
            }

            return (IGenericRepository<T>)_repositories[type];
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public ILifestyleRepository LifestyleRepository => _lifestyleRepository ??= new LifestyleRepository(_context);
        
     
    }
} 