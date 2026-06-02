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
        private readonly IPhotoService _photoService; // 1. KHAI BÁO THÊM PHOTOSERVICE
        public ReportService(IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;   
        }
        public async Task<bool> SubmitReportAsync(CreateReportRequest request)
        {
            // Validate cơ bản
            if (request.ReportedUserId == null && request.ReportedRoomId == null)
            {
                throw new ArgumentException("Phải chỉ định người dùng hoặc phòng bị report.");
            }
            var imageUrls = new List<string>();
            if (request.Images != null && request.Images.Any())
            {
                foreach (var file in request.Images)
                {
                    if (file.Length > 0)
                    {
                        // Upload lên AWS S3 vào thư mục "reports"
                        var url = await _photoService.UploadPhotoAsync(file, "reports");
                        imageUrls.Add(url);
                    }
                }
            }

            // Có thể thêm logic kiểm tra User/Room có tồn tại trong DB không
            var reporterExists = await _unitOfWork.Repository<Account>().GetByIdAsync(request.ReporterId) != null;
            if (!reporterExists)
            {
                throw new ArgumentException("Người thực hiện report không tồn tại.");
            }

            Guid? reportedUserId = request.ReportedUserId;
            if (request.ReportedRoomId.HasValue)
            {
                var room = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(request.ReportedRoomId.Value);
                if (room == null)
                {
                    throw new ArgumentException("Tin phòng trọ không tồn tại.");
                }
                // Báo cáo phòng: lưu luôn chủ trọ (UserId của tin) để admin/DB thấy ReportedUserId.
                reportedUserId = room.UserId;
            }

            var report = new Report
            {
                ReporterId = request.ReporterId,
                ReportedUserId = reportedUserId,
                ReportedRoomId = request.ReportedRoomId,
                Reason = request.Reason,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                Images = imageUrls,
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

            var reports = await query
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedRoom)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var ownerIds = reports
                .Where(r => r.ReportedUser == null && r.ReportedRoom != null)
                .Select(r => r.ReportedRoom!.UserId)
                .Distinct()
                .ToList();

            var ownerNames = ownerIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _unitOfWork.Repository<Account>().GetQueryable()
                    .Where(a => ownerIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, a => a.UserName ?? a.Email ?? "—");

            return reports.Select(r =>
            {
                var roomOwnerId = r.ReportedRoom?.UserId;
                var resolvedUserId = r.ReportedUserId ?? roomOwnerId;
                string? resolvedUserName = r.ReportedUser?.UserName;
                if (string.IsNullOrWhiteSpace(resolvedUserName) && roomOwnerId.HasValue && ownerNames.TryGetValue(roomOwnerId.Value, out var ownerName))
                {
                    resolvedUserName = ownerName;
                }

                return new ReportResponseDTO
                {
                    ReportId = r.ReportId,
                    ReporterName = r.Reporter != null ? r.Reporter.UserName : "Unknown",
                    ReportedUserId = resolvedUserId,
                    ReportedUserName = resolvedUserName,
                    ReportedRoomId = r.ReportedRoomId,
                    ReportedRoomName = r.ReportedRoom != null ? r.ReportedRoom.Title : null,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt,
                    Images = r.Images
                };
            }).ToList();
        }
    }
}
