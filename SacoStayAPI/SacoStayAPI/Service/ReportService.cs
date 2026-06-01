using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
       
        public ReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }
        public async Task<bool> SubmitReportAsync(CreateReportRequest request)
        {
            // Validate cơ bản
            if (request.ReportedUserId == null && request.ReportedRoomId == null)
            {
                throw new ArgumentException("Phải chỉ định người dùng hoặc phòng bị report.");
            }

            // Có thể thêm logic kiểm tra User/Room có tồn tại trong DB không
            var reporterExists = await _unitOfWork.Repository<Account>().GetByIdAsync(request.ReporterId) != null;
            if (!reporterExists)
            {
                throw new ArgumentException("Người thực hiện report không tồn tại.");
            }

            var report = new Report
            {
                ReporterId = request.ReporterId,
                ReportedUserId = request.ReportedUserId,
                ReportedRoomId = request.ReportedRoomId,
                Reason = request.Reason,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            // Thêm vào DbContext thông qua Repository
            await _unitOfWork.Repository<Report>().AddAsync(report);

            // Lưu thay đổi vào database
            var result = await _unitOfWork.CompleteAsync();

            return result > 0;
        }
        public async Task<IEnumerable<ReportResponseDTO>> GetListReportsAsync()
        {
            // 1. Lấy IQueryable từ UoW ( KHÔNG CÓ AWAIT Ở ĐÂY )
            var query = _unitOfWork.Repository<Report>().GetQueryable();

            // 2. Thực hiện Include, Select và ToListAsync ( CÓ AWAIT Ở ĐÂY )
            var reports = await query
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedRoom)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReportResponseDTO
                {
                    ReportId = r.ReportId,
                    ReporterName = r.Reporter != null ? r.Reporter.UserName : "Unknown",
                    ReportedUserName = r.ReportedUser != null ? r.ReportedUser.UserName : null,
                    ReportedRoomName = r.ReportedRoom != null ? r.ReportedRoom.Title : null,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return reports;
        }
    }
}
