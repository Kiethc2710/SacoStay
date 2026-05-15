using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class LifestyleService
    {
        private readonly IUnitOfWork _unitOfWork;
        public LifestyleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        //lấy tất cả câu hỏi về lối sống kèm theo các lựa chọn
        //public async Task<IEnumerable<LifestyleQuestion>> GetAllLifestyleQuestionsAsync()
        //{
        //    return await _unitOfWork.LifestyleRepository.GetAllWithOptionsAsync();
        //}

        public async Task CreateQuestionWithOptionsAsync(CreateQuestionDTO dto)
        {
            // Khởi tạo Entity LifestyleQuestion
            var newQuestion = new LifestyleQuestion
            {
                Content = dto.Content,

                // Map danh sách string thành danh sách Entity LifestyleOption
                Options = dto.Options.Select(optionText => new LifestyleOption
                {
                    Content = optionText
                }).ToList()
            };

            // Thêm vào DbSet thông qua Repository
            await _unitOfWork.Repository<LifestyleQuestion>().AddAsync(newQuestion);

            // Commit thay đổi xuống Database
            await _unitOfWork.CompleteAsync();
        }
    }
}
