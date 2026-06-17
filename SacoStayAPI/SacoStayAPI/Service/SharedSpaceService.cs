using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class SharedSpaceService : ISharedSpaceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SharedSpaceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        // TASK 1: Khởi tạo kênh trọ chung sau khi Matching Engine xác nhận 2 người quẹt trúng nhau
        public async Task<(bool IsSuccess, string Message, Guid? SpaceId)> CreateSharedSpaceAsync(Guid user1Id, Guid user2Id)
        {
            // 1. Kiểm tra xem giữa 2 ông này đã có Không gian chung nào đang ACTIVE (đang chạy) chưa
            var existingSpace = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .FirstOrDefaultAsync(s => ((s.User1Id == user1Id && s.User2Id == user2Id)
                                       || (s.User1Id == user2Id && s.User2Id == user1Id))
                                       && s.Status == "Active");

            if (existingSpace != null)
            {
                return (true, "Không gian chung giữa hai người dùng này đã tồn tại và đang hoạt động.", existingSpace.Id);
            }

            // 2. Nếu chưa có thì tiến hành tạo mới tinh
            var newSpace = new SharedSpace
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                Status = "Active", // Mặc định tạo ra là Active để hai bên tương tác
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<SharedSpace>().AddAsync(newSpace);

            // Lưu xuống DB thông qua Unit of Work
            var isSaved = await _unitOfWork.CompleteAsync() > 0;

            return isSaved
                ? (true, "Khởi tạo không gian trọ chung cho 2 người thành công!", newSpace.Id)
                : (false, "Gặp lỗi hệ thống khi lưu không gian chung.", null);
        }
        // TASK 2: Lấy không gian chung hiện tại và TỰ ĐỘNG PHÂN BIỆT + LẤY TÊN USER
        public async Task<object?> GetCurrentSpaceAsync(Guid userId)
        {
            // Bước 1: Lấy thông tin Space và Shortlist phòng trọ như cũ
            var spaceData = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .Where(s => (s.User1Id == userId || s.User2Id == userId) && s.Status == "Active")
                .Select(s => new
                {
                    s.Id,
                    s.User1Id,
                    s.User2Id,
                    s.Status,
                    s.CreatedAt,
                    Shortlist = s.Shortlists.Select(sl => new
                    {
                        sl.Id,
                        sl.RoomId,
                        RoomTitle = sl.Room.Title,
                        RoomCategory = sl.Room.Category,
                        sl.Room.Price,
                        Address = $"{sl.Room.DetailedAddress}, {sl.Room.District}, {sl.Room.City}",
                        IsAddedByMe = sl.AddedByUserId == userId,
                        MyVote = sl.Votes.Where(v => v.UserId == userId).Select(v => v.VoteStatus).FirstOrDefault() ?? "None",
                        PartnerVote = sl.Votes.Where(v => v.UserId != userId).Select(v => v.VoteStatus).FirstOrDefault() ?? "None"
                    })
                })
                .FirstOrDefaultAsync();

            // Nếu không nằm trong không gian chung nào thì out luôn
            if (spaceData == null) return null;

            // Xác định chính xác Guid ID của đứa bồ tèo chung phòng với mình
            Guid partnerId = spaceData.User1Id == userId ? spaceData.User2Id : spaceData.User1Id;

            // -------------------------------------------------------------------------
            // -------------------------------------------------------------------------
            // Bước 2: Truy vấn vào bảng Account để lấy và ghép FirstName + LastName
            // -------------------------------------------------------------------------
            var myName = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => u.Id == userId)
                .Select(u => $"{u.FirstName} {u.LastName}") // ✨ Ghép chuỗi trực tiếp bằng String Interpolation
                .FirstOrDefaultAsync() ?? "Tôi";

            var partnerName = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => u.Id == partnerId)
                .Select(u => $"{u.FirstName} {u.LastName}") // ✨ Ghép tương tự cho đối tác (Partner)
                .FirstOrDefaultAsync() ?? "Bạn cùng phòng";

            // Bước 3: Gộp tất cả lại thành 1 Object hoàn chỉnh trả về cho Front-End
            return new
            {
                spaceData.Id,
                MyId = userId,
                MyName = myName,               // ✨ Đã có Tên của Tôi
                PartnerId = partnerId,
                PartnerName = partnerName,     // ✨ Đã có Tên của Đối tác
                spaceData.Status,
                spaceData.CreatedAt,
                spaceData.Shortlist
            };
        }

        // TASK 3: Thêm phòng vào Shortlist (Validate loại hình phòng + Auto Like cho người thêm)
        public async Task<(bool IsSuccess, string Message)> AddToShortlistAsync(Guid userId, Guid spaceId, AddToShortlistDTO dto)
        {
            var space = await _unitOfWork.Repository<SharedSpace>().GetByIdAsync(spaceId);
            if (space == null || space.Status != "Active" || (space.User1Id != userId && space.User2Id != userId))
                return (false, "Không gian chung không tồn tại hoặc bạn không có quyền truy cập.");

            // Kiểm tra phòng trọ có tồn tại không
            var room = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(dto.RoomId);
            if (room == null) return (false, "Phòng trọ không tồn tại trên hệ thống.");

            // Kiểm tra trùng lặp trong danh sách Shortlist hiện tại
            var isDuplicate = await _unitOfWork.Repository<SpaceShortlist>().GetQueryable()
                .AnyAsync(sl => sl.SpaceId == spaceId && sl.RoomId == dto.RoomId);
            if (isDuplicate) return (false, "Phòng trọ này đã nằm trong danh sách cân nhắc chung.");

            // Khởi tạo bản ghi Shortlist mới
            var shortlistEntry = new SpaceShortlist
            {
                Id = Guid.NewGuid(),
                SpaceId = spaceId,
                RoomId = dto.RoomId,
                AddedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<SpaceShortlist>().AddAsync(shortlistEntry);

            // LOGIC PHỤ: Tự động tạo luôn bản ghi Vote 'Like' cho chính người vừa thêm phòng
            var autoVote = new RoomVote
            {
                Id = Guid.NewGuid(),
                ShortlistId = shortlistEntry.Id,
                UserId = userId,
                VoteStatus = "Like",
                UpdatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<RoomVote>().AddAsync(autoVote);

            return await _unitOfWork.CompleteAsync() > 0
                ? (true, $"Đã thêm {room.Category} vào danh sách. Hệ thống tự động ghi nhận lượt Thích của bạn.")
                : (false, "Lỗi hệ thống khi lưu danh sách Shortlist.");
        }

        // TASK 4: Xử lý logic biểu quyết Vote (Like/Dislike)
        public async Task<(bool IsSuccess, string Message)> VoteRoomAsync(Guid userId, Guid shortlistId, VoteRoomDTO dto)
        {
            var shortlist = await _unitOfWork.Repository<SpaceShortlist>().GetQueryable()
                .Include(sl => sl.Space)
                .FirstOrDefaultAsync(sl => sl.Id == shortlistId);

            if (shortlist == null || shortlist.Space.Status != "Active")
                return (false, "Bản ghi danh sách phòng không khả dụng.");

            if (shortlist.Space.User1Id != userId && shortlist.Space.User2Id != userId)
                return (false, "Bạn không thuộc không gian chung chứa phòng trọ này.");

            // Kiểm tra xem User này đã từng vote cho phòng này trong không gian này chưa
            var existingVote = await _unitOfWork.Repository<RoomVote>().GetQueryable()
                .FirstOrDefaultAsync(v => v.ShortlistId == shortlistId && v.UserId == userId);

            if (existingVote != null)
            {
                // Cập nhật lại nếu đổi ý kiến vote
                existingVote.VoteStatus = dto.VoteStatus;
                existingVote.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Repository<RoomVote>().Update(existingVote);
            }
            else
            {
                // Tạo lượt vote mới tinh
                var newVote = new RoomVote
                {
                    Id = Guid.NewGuid(),
                    ShortlistId = shortlistId,
                    UserId = userId,
                    VoteStatus = dto.VoteStatus,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<RoomVote>().AddAsync(newVote);
            }

            return await _unitOfWork.CompleteAsync() > 0
                ? (true, $"Ghi nhận biểu quyết thành công: {dto.VoteStatus} phòng trọ.")
                : (false, "Gặp lỗi hệ thống khi lưu nhận định biểu quyết.");
        }

        // TASK 5: Xử lý đóng/chốt phòng trọ chung (Finalize)
        //public async Task<(bool IsSuccess, string Message)> FinalizeSpaceAsync(Guid userId, Guid spaceId, FinalizeSpaceDTO dto)
        //{
        //    var space = await _unitOfWork.Repository<SharedSpace>().GetByIdAsync(spaceId);
        //    if (space == null || space.Status != "Active" || (space.User1Id != userId && space.User2Id != userId))
        //        return (false, "Hồ sơ không gian chung không hợp lệ hoặc đã đóng.");

        //    // Kiểm tra phòng muốn chốt có nằm trong Shortlist của không gian này không
        //    var shortlist = await _unitOfWork.Repository<SpaceShortlist>().GetQueryable()
        //        .FirstOrDefaultAsync(sl => sl.Id == dto.ShortlistId && sl.SpaceId == spaceId);
        //    if (shortlist == null) return (false, "Phòng trọ này không nằm trong danh sách cân nhắc chung.");

        //    // Đếm số lượng lượt LIKE thực tế của phòng này
        //    var likeCount = await _unitOfWork.Repository<RoomVote>().GetQueryable()
        //        .CountAsync(v => v.ShortlistId == dto.ShortlistId && v.VoteStatus == "Like");

        //    // Điều kiện bắt buộc: Phải đạt đủ 2/2 lượt LIKE từ cả hai người mới cho phép chốt
        //    if (likeCount < 2)
        //        return (false, "Không thể chốt! Phòng trọ này chưa đạt được sự đồng thuận (2/2 lượt Like) từ cả hai bên.");

        //    // Thực hiện đóng không gian chung và lưu phòng trọ được lựa chọn cuối cùng
        //    space.Status = "Finalized";
        //    space.FinalizedRoomId = shortlist.RoomId;
        //    _unitOfWork.Repository<SharedSpace>().Update(space);

        //    return await _unitOfWork.CompleteAsync() > 0
        //        ? (true, "Chúc mừng! Hai bạn đã thống nhất chốt phòng trọ này thành công.")
        //        : (false, "Lỗi hệ thống trong quá trình thực hiện khóa sổ chốt phòng.");
        //}
        // =========================================================================
        // BÊN A BẤM NÚT ĐỀ XUẤT CHỐT PHÒNG (Trạng thái chuyển sang PendingFinalize)
        // =========================================================================
        public async Task<(bool IsSuccess, string Message)> ProposeFinalizeAsync(Guid userId, Guid spaceId, FinalizeSpaceDTO dto)
        {
            var space = await _unitOfWork.Repository<SharedSpace>().GetByIdAsync(spaceId);
            if (space == null || space.Status != "Active")
                return (false, "Không gian chung không hợp lệ hoặc đã nằm trong quy trình chốt phòng.");

            if (space.User1Id != userId && space.User2Id != userId)
                return (false, "Bạn không thuộc không gian trọ này.");

            var totalLikes = await _unitOfWork.Repository<RoomVote>().GetQueryable()
                .CountAsync(v => v.ShortlistId == dto.ShortlistId && v.VoteStatus == "Like");

            if (totalLikes < 2)
                return (false, "Phòng trọ này chưa đạt đủ 2 lượt Thích từ cả hai bên để có thể đề xuất chốt.");

            var shortlist = await _unitOfWork.Repository<SpaceShortlist>().GetByIdAsync(dto.ShortlistId);

            space.Status = "PendingFinalize";
            space.FinalizedRoomId = shortlist!.RoomId;
            space.FinalizeRequestedByUserId = userId;

            _unitOfWork.Repository<SharedSpace>().Update(space);
            return await _unitOfWork.CompleteAsync() > 0
                ? (true, "Đã gửi yêu cầu chốt phòng thành công. Đang chờ bạn cùng phòng của bạn phê duyệt.")
                : (false, "Lỗi hệ thống khi xử lý đề xuất chốt.");
        }

        // =========================================================================
        // BÊN B BẤM NÚT "ĐỒNG Ý" (CONFIRM) -> CHÍNH THỨC CHỐT SỔ (Trạng thái thành Finalized)
        // =========================================================================
        public async Task<(bool IsSuccess, string Message)> AcceptFinalizeAsync(Guid userId, Guid spaceId)
        {
            var space = await _unitOfWork.Repository<SharedSpace>().GetByIdAsync(spaceId);
            if (space == null || space.Status != "PendingFinalize")
                return (false, "Không có đề xuất chốt phòng nào đang chờ duyệt.");

            if (space.FinalizeRequestedByUserId == userId)
                return (false, "Hệ thống đang chờ đối tác của bạn phê duyệt, bạn không thể tự duyệt yêu cầu của mình.");

            if (space.User1Id != userId && space.User2Id != userId)
                return (false, "Bạn không có quyền can thiệp vào phòng này.");

            space.Status = "Finalized";

            _unitOfWork.Repository<SharedSpace>().Update(space);
            return await _unitOfWork.CompleteAsync() > 0
                ? (true, "Xác nhận chốt phòng trọ thành công! Kênh tìm phòng chung chính thức đóng hòm.")
                : (false, "Lỗi hệ thống khi phê duyệt chốt phòng.");
        }

        // =========================================================================
        // BÊN B BẤM NÚT "TỪ CHỐI" (REJECT) -> HỦY LỆNH ĐỀ XUẤT (Hoàn nguyên trạng thái về Active)
        // =========================================================================
        public async Task<(bool IsSuccess, string Message)> RejectFinalizeAsync(Guid userId, Guid spaceId)
        {
            var space = await _unitOfWork.Repository<SharedSpace>().GetByIdAsync(spaceId);
            if (space == null || space.Status != "PendingFinalize")
                return (false, "Không có đề xuất chốt phòng nào để từ chối.");

            if (space.User1Id != userId && space.User2Id != userId)
                return (false, "Bạn không có quyền can thiệp.");

            space.Status = "Active";
            space.FinalizedRoomId = null;
            space.FinalizeRequestedByUserId = null;

            _unitOfWork.Repository<SharedSpace>().Update(space);
            return await _unitOfWork.CompleteAsync() > 0
                ? (true, "Đã từ chối đề xuất chốt phòng. Kênh tương tác chung đã được mở lại để hai bạn tiếp tục lọc trọ.")
                : (false, "Lỗi hệ thống khi hủy lệnh đề xuất.");
        }
    }
}