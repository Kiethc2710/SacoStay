using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Repositories
{
    public interface ILifestyleRepository : IGenericRepository<LifestyleQuestion>
    {
            Task<IEnumerable<LifestyleQuestion>> GetAllWithOptionsAsync();
    }
}
