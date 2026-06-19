using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;

namespace SacoStayAPI.Service
{
    public class SharedSpaceService : ISharedSpaceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationDispatcher _notificationDispatcher;

        public SharedSpaceService(IUnitOfWork unitOfWork, INotificationDispatcher notificationDispatcher)
        {
            _unitOfWork = unitOfWork;
            _notificationDispatcher = notificationDispatcher;
        }
        // TASK 1: Khởi tạo kênh trọ chung sau khi Matching Engine xác nhận 2 người quẹt trúng nhau
        public async Task<(bool IsSuccess, string Message, Guid? SpaceId)> CreateSharedSpaceAsync(Guid user1Id, Guid user2Id)
        {
            if (user1Id == user2Id)
                return (false, "Không thể tạo không gian chung với chính mình.", null);

            // Đối tác đã chốt phòng với ai đó — không cho tạo không gian mới với họ
            var targetHasFinalized = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .AnyAsync(s => (s.User1Id == user2Id || s.User2Id == user2Id) && s.Status == "Finalized");

            if (targetHasFinalized)
            {
                return (false, "Người này đã hoàn tất quá trình tìm phòng và chốt trọ với bạn cùng phòng khác.", null);
            }

            // Kiểm tra không gian đã tồn tại giữa hai người (mọi trạng thái trừ Cancelled)
            var existingPair = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .FirstOrDefaultAsync(s => ((s.User1Id == user1Id && s.User2Id == user2Id)
                                       || (s.User1Id == user2Id && s.User2Id == user1Id))
                                       && s.Status != "Cancelled");

            if (existingPair != null)
            {
                if (existingPair.Status == "Finalized")
                    return (false, "Hai bạn đã chốt phòng trọ trong không gian chung này trước đó.", null);

                return (true, "Không gian chung giữa hai người dùng này đã tồn tại.", existingPair.Id);
            }

            var newSpace = new SharedSpace
            {
                Id = Guid.NewGuid(),
                User1Id = user1Id,
                User2Id = user2Id,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<SharedSpace>().AddAsync(newSpace);

            var isSaved = await _unitOfWork.CompleteAsync() > 0;

            if (isSaved)
            {
                var creatorName = await DisplayNameForUserAsync(user1Id);
                await _notificationDispatcher.NotifyAsync(
                    user2Id,
                    "Không gian chung mới",
                    $"{creatorName} đã tạo không gian tìm trọ chung với bạn",
                    "shared_space",
                    $"/shared-space?spaceId={newSpace.Id}");
            }

            return isSaved
                ? (true, "Khởi tạo không gian trọ chung cho 2 người thành công!", newSpace.Id)
                : (false, "Gặp lỗi hệ thống khi lưu không gian chung.", null);
        }

        public async Task<IReadOnlyList<object>> GetUserSpacesAsync(Guid userId)
        {
            var spaces = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .Where(s => (s.User1Id == userId || s.User2Id == userId)
                    && (s.Status == "Active" || s.Status == "PendingFinalize" || s.Status == "Finalized"))
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.User1Id,
                    s.User2Id,
                    s.Status,
                    s.CreatedAt,
                    s.FinalizedRoomId,
                    ShortlistRoomIds = s.Shortlists.Select(sl => sl.RoomId).ToList()
                })
                .ToListAsync();

            if (spaces.Count == 0) return Array.Empty<object>();

            var partnerIds = spaces
                .Select(s => s.User1Id == userId ? s.User2Id : s.User1Id)
                .Distinct()
                .ToList();

            var partnerNames = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => partnerIds.Contains(u.Id))
                .Select(u => new { u.Id, Name = $"{u.FirstName} {u.LastName}" })
                .ToDictionaryAsync(x => x.Id, x => x.Name);

            return spaces.Select(s =>
            {
                var partnerId = s.User1Id == userId ? s.User2Id : s.User1Id;
                return (object)new
                {
                    s.Id,
                    PartnerId = partnerId,
                    PartnerName = partnerNames.GetValueOrDefault(partnerId) ?? "Bạn cùng phòng",
                    s.Status,
                    s.CreatedAt,
                    s.FinalizedRoomId,
                    s.ShortlistRoomIds
                };
            }).ToList();
        }

        public async Task<object?> GetSpaceByIdAsync(Guid userId, Guid spaceId)
        {
            var spaceData = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .Where(s => s.Id == spaceId
                    && (s.User1Id == userId || s.User2Id == userId)
                    && (s.Status == "Active" || s.Status == "PendingFinalize" || s.Status == "Finalized"))
                .Select(s => new
                {
                    s.Id,
                    s.User1Id,
                    s.User2Id,
                    s.Status,
                    s.CreatedAt,
                    s.FinalizedRoomId,
                    s.FinalizeRequestedByUserId,
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

            if (spaceData == null) return null;

            return await MapSpaceResponseAsync(userId, spaceData.Id, spaceData.User1Id, spaceData.User2Id,
                spaceData.Status, spaceData.CreatedAt, spaceData.FinalizedRoomId, spaceData.FinalizeRequestedByUserId,
                spaceData.Shortlist);
        }
        // TASK 2: Lấy không gian chung hiện tại và TỰ ĐỘNG PHÂN BIỆT + LẤY TÊN USER
        public async Task<object?> GetCurrentSpaceAsync(Guid userId)
        {
            var spaceData = await _unitOfWork.Repository<SharedSpace>().GetQueryable()
                .Where(s => (s.User1Id == userId || s.User2Id == userId)
                    && (s.Status == "Active" || s.Status == "PendingFinalize" || s.Status == "Finalized"))
                .OrderByDescending(s => s.Status == "Active")
                .ThenByDescending(s => s.Status == "PendingFinalize")
                .ThenByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.User1Id,
                    s.User2Id,
                    s.Status,
                    s.CreatedAt,
                    s.FinalizedRoomId,
                    s.FinalizeRequestedByUserId,
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

            if (spaceData == null) return null;

            return await MapSpaceResponseAsync(userId, spaceData.Id, spaceData.User1Id, spaceData.User2Id,
                spaceData.Status, spaceData.CreatedAt, spaceData.FinalizedRoomId, spaceData.FinalizeRequestedByUserId,
                spaceData.Shortlist);
        }

        private async Task<object> MapSpaceResponseAsync(
            Guid userId,
            Guid spaceId,
            Guid user1Id,
            Guid user2Id,
            string status,
            DateTime createdAt,
            Guid? finalizedRoomId,
            Guid? finalizeRequestedByUserId,
            IEnumerable<object> shortlist)
        {
            Guid partnerId = user1Id == userId ? user2Id : user1Id;

            var myName = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => u.Id == userId)
                .Select(u => $"{u.FirstName} {u.LastName}")
                .FirstOrDefaultAsync() ?? "Tôi";

            var partnerName = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => u.Id == partnerId)
                .Select(u => $"{u.FirstName} {u.LastName}")
                .FirstOrDefaultAsync() ?? "Bạn cùng phòng";

            return new
            {
                Id = spaceId,
                MyId = userId,
                MyName = myName,
                PartnerId = partnerId,
                PartnerName = partnerName,
                Status = status,
                CreatedAt = createdAt,
                FinalizedRoomId = finalizedRoomId,
                FinalizeRequestedByUserId = finalizeRequestedByUserId,
                Shortlist = shortlist
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
            var isSaved = await _unitOfWork.CompleteAsync() > 0;

            if (isSaved)
            {
                var partnerId = space.User1Id == userId ? space.User2Id : space.User1Id;
                var proposerName = await DisplayNameForUserAsync(userId);
                var room = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(shortlist!.RoomId);
                var roomTitle = room?.Title ?? "một phòng trọ";
                await _notificationDispatcher.NotifyAsync(
                    partnerId,
                    "Đề xuất chốt phòng",
                    $"{proposerName} đề xuất chốt phòng \"{roomTitle}\" trong không gian chung. Hãy xem và phản hồi nhé!",
                    "shared_space_finalize",
                    $"/shared-space?spaceId={spaceId}");
            }

            return isSaved
                ? (true, "Đã gửi yêu cầu chốt phòng thành công. Đang chờ bạn cùng phòng của bạn phê duyệt.")
                : (false, "Lỗi hệ thống khi xử lý đề xuất chốt.");
        }

        private async Task<string> DisplayNameForUserAsync(Guid userId)
        {
            var name = await _unitOfWork.Repository<Account>().GetQueryable()
                .Where(u => u.Id == userId)
                .Select(u => $"{u.FirstName} {u.LastName}")
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(name)) return name.Trim();
            return "Người dùng";
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

            var proposerId = space.FinalizeRequestedByUserId;
            var finalizedRoomId = space.FinalizedRoomId;

            space.Status = "Finalized";

            _unitOfWork.Repository<SharedSpace>().Update(space);
            var isSaved = await _unitOfWork.CompleteAsync() > 0;

            if (isSaved && proposerId.HasValue)
            {
                var approverName = await DisplayNameForUserAsync(userId);
                var roomTitle = "một phòng trọ";
                if (finalizedRoomId.HasValue)
                {
                    var room = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(finalizedRoomId.Value);
                    if (!string.IsNullOrWhiteSpace(room?.Title)) roomTitle = room!.Title;
                }
                await _notificationDispatcher.NotifyAsync(
                    proposerId.Value,
                    "Đã chốt phòng",
                    $"{approverName} đã đồng ý chốt phòng \"{roomTitle}\" trong không gian chung.",
                    "shared_space_finalized",
                    $"/shared-space?spaceId={spaceId}");
            }

            return isSaved
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

            var partnerId = space.User1Id == userId ? space.User2Id : space.User1Id;

            space.Status = "Active";
            space.FinalizedRoomId = null;
            space.FinalizeRequestedByUserId = null;

            _unitOfWork.Repository<SharedSpace>().Update(space);
            var isSaved = await _unitOfWork.CompleteAsync() > 0;

            if (isSaved)
            {
                var rejecterName = await DisplayNameForUserAsync(userId);
                await _notificationDispatcher.NotifyAsync(
                    partnerId,
                    "Từ chối chốt phòng",
                    $"{rejecterName} đã từ chối đề xuất chốt phòng trong không gian chung.",
                    "shared_space_finalize_reject",
                    $"/shared-space?spaceId={spaceId}");
            }

            return isSaved
                ? (true, "Đã từ chối đề xuất chốt phòng. Kênh tương tác chung đã được mở lại để hai bạn tiếp tục lọc trọ.")
                : (false, "Lỗi hệ thống khi hủy lệnh đề xuất.");
        }
    }
}