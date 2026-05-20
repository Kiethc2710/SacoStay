using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;
using SacoStayAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public class RoomPostService : IRoomPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPhotoService _photoService;

        // Định nghĩa chính xác 10 tiện nghi xuất hiện trên giao diện UI để đối chiếu dữ liệu
        private static readonly HashSet<string> AllowedAmenities = new()
        {
            "Điều hòa", "Nóng lạnh", "Máy giặt",
            "Ban công", "Thang máy", "Bếp riêng",
            "Bảo vệ 24/7", "Chỗ để xe", "WiFi", "Tủ lạnh"
        };

        public RoomPostService(IUnitOfWork unitOfWork, IPhotoService photoService)
        {
            _unitOfWork = unitOfWork;
            _photoService = photoService;
        }

        public async Task<RoomPost> CreatePostAsync(CreateRoomPostDTO dto, Guid userId)
        {
            var lat = dto.Location.Latitude;
            var lng = dto.Location.Longitude;

            // 1. Kiểm tra vị trí địa lý (Geofencing Việt Nam)
            if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
                throw new ArgumentException("Tọa độ địa lý không hợp lệ toàn cầu.");

            if (lat < 8.4 || lat > 23.4 || lng < 102.1 || lng > 109.5)
                throw new ArgumentException("Vị trí ghim phải nằm trong phạm vi lãnh thổ Việt Nam.");

            // 2. Kiểm tra ô "Mô tả chi tiết"
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Vui lòng nhập mô tả chi tiết cho phòng trọ (giờ giấc, chi phí...).");

            if (dto.Description.Length < 10)
                throw new ArgumentException("Nội dung mô tả quá ngắn. Vui lòng nhập chi tiết hơn.");

            // 3. LOGIC KIỂM TRA TIỆN NGHI (Khớp chuẩn dữ liệu từ Checkbox UI)
            if (dto.Amenities != null && dto.Amenities.Any())
            {
                foreach (var amenity in dto.Amenities)
                {
                    // Nếu frontend gửi lên một chuỗi không nằm trong 10 mục giao diện -> Báo lỗi ngay
                    if (!AllowedAmenities.Contains(amenity))
                    {
                        throw new ArgumentException($"Tiện nghi '{amenity}' không hợp lệ hoặc không nằm trong danh sách hỗ trợ của hệ thống.");
                    }
                }
            }

            // 4. Kiểm tra số lượng hình ảnh phòng trọ
            if (dto.ImageFiles == null || dto.ImageFiles.Count == 0)
                throw new ArgumentException("Bài đăng phòng trọ bắt buộc phải có ít nhất 1 hình ảnh.");

            if (dto.ImageFiles.Count > 5)
                throw new ArgumentException("Bạn chỉ được phép tải lên tối đa 5 hình ảnh.");

            // 5. Xử lý tải mảng ảnh lên S3
            var uploadedImageUrls = new List<string>();
            foreach (var file in dto.ImageFiles)
            {
                if (file.Length > 0)
                {
                    var url = await _photoService.UploadPhotoAsync(file, "rooms");
                    uploadedImageUrls.Add(url);
                }
            }

            // 6. Mapping dữ liệu vào Entity để chuẩn bị lưu xuống DB
            var roomPost = new RoomPost
            {
                Title = dto.Title,
                Description = dto.Description, // Lưu nội dung văn bản từ ô "Mô tả chi tiết"
                DetailedAddress = dto.DetailedAddress,
                City = string.IsNullOrEmpty(dto.City) ? "Việt Nam" : dto.City,
                District = dto.District,
                Area = dto.Area,
                MaxPeople = dto.MaxPeople,
                Price = dto.Price,
                Latitude = lat,
                Longitude = lng,
                Images = uploadedImageUrls,
                Amenities = dto.Amenities, // Lưu mảng các tiện nghi hợp lệ đã chọn
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            // 7. Thực thi lưu trữ qua UnitOfWork
            await _unitOfWork.Repository<RoomPost>().AddAsync(roomPost);
            await _unitOfWork.CompleteAsync();

            return roomPost;
        }

        public async Task<IEnumerable<RoomPost>> GetMyPostsAsync(Guid userId)
        {
            var posts = await _unitOfWork.Repository<RoomPost>().FindAsync(p => p.UserId == userId);
            return posts.OrderByDescending(p => p.CreatedAt);
        }

        public async Task<IEnumerable<object>> GetRoomsNearbyAsync(double userLat, double userLng, double radiusInKm)
        {
            var dbs = await _unitOfWork.Repository<RoomPost>().FindAsync(p => p.Status == "Active");

            return dbs.Select(room => new {
                Room = room,
                Distance = CalculateHaversineDistance(userLat, userLng, room.Latitude, room.Longitude)
            })
                .Where(x => x.Distance <= radiusInKm)
                .OrderBy(x => x.Distance)
                .Select(x => new {
                    x.Room.Id,
                    x.Room.UserId,
                    x.Room.Title,
                    x.Room.Price,
                    x.Room.DetailedAddress,
                    x.Room.Images,
                    x.Room.Amenities,
                    Location = new { x.Room.Latitude, x.Room.Longitude },
                    DistanceKm = Math.Round(x.Distance, 2)
                });
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Bán kính Trái Đất (km)
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => (Math.PI / 180) * angle;
        public async Task RecordViewAsync(Guid roomPostId, Guid tenantId)
        {
            var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(roomPostId);
            if (roomPost == null) return; // Bài viết không tồn tại thì bỏ qua

            // Không ghi nhận lượt xem nếu chính chủ trọ tự bấm vào xem bài đăng của mình
            if (roomPost.UserId == tenantId) return;

            var viewHistory = new RoomViewHistory
            {
                RoomPostId = roomPostId,
                TenantId = tenantId,
                ViewedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<RoomViewHistory>().AddAsync(viewHistory);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<object> GetRoomAnalyticsAsync(Guid roomPostId, Guid userId)
        {
            var roomPost = await _unitOfWork.Repository<RoomPost>().GetByIdAsync(roomPostId);
            if (roomPost == null) throw new ArgumentException("Bài đăng không tồn tại.");
            if (roomPost.UserId != userId) throw new UnauthorizedAccessException("Bạn không có quyền xem phân tích bài đăng của người khác.");

            // 1. Truy vấn toàn bộ lịch sử xem tin của bài viết này trong vòng 24 giờ qua
            var oneDayAgo = DateTime.UtcNow.AddHours(-24);
            var allViews = await _unitOfWork.Repository<RoomViewHistory>().FindAsync(v => v.RoomPostId == roomPostId && v.ViewedAt >= oneDayAgo);

            // Đổ danh sách người xem và thực hiện Join thủ công hoặc lấy qua UserManager của tầng ngoài để hiển thị lên UI
            // Để tối ưu hiệu năng, ta GroupBy TenantId để đếm lượt của cùng một người xem tin
            var queryHistory = allViews
                .OrderByDescending(v => v.ViewedAt)
                .Select(v => new
                {
                    TenantId = v.TenantId,
                    ViewedTime = v.ViewedAt
                }).ToList();

            // 2. Thực hiện nghiệp vụ giới hạn phân quyền theo gói thiết kế (Mấu chốt logic)
            var currentPackage = roomPost.PackageTier.ToUpper();
            bool isLimited = currentPackage != "ELITE"; // Không phải gói ELITE thì bị bóp hiển thị còn 5 người

            var finalHistoryResult = isLimited ? queryHistory.Take(5).ToList() : queryHistory;

            return new
            {
                RoomId = roomPost.Id,
                RoomTitle = roomPost.Title,
                CurrentPackage = currentPackage,
                IsLimitedView = isLimited,
                TotalViewsIn24H = queryHistory.Count,
                Viewers = finalHistoryResult // Trả về danh sách TenantId và thời gian xem tin để frontend map profile khách hàng
            };
        }
    }
}