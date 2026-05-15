using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Repositories
{
    public class LifestyleRepository : GenericRepository<LifestyleQuestion>, ILifestyleRepository
    {
        private readonly ApplicationDBContext _context;

        public LifestyleRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }
        //lấy tất cả câu hỏi về lối sống kèm theo các lựa chọn
        public async Task<IEnumerable<LifestyleQuestion>> GetAllWithOptionsAsync()
        {
            return await _context.LifestyleQuestions
                                 .Include(q => q.Options)
                                 .ToListAsync();
        }
        
    }
}
