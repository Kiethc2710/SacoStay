using System;
using System.Collections.Generic;

namespace SacoStayAPI.Model.Entities
{
    public class RoomPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailedAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public double Area { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public List<string> Amenities { get; set; } = new List<string>();
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- CẬP NHẬT QUẢN LÝ TRẠNG THÁI GÓI TIN ĐĂNG ---
        public string Status { get; set; } = "PendingPayment"; // Mặc định: PendingPayment, PendingApproval, Active, Hidden
        public string PackageTier { get; set; } = "BASIC"; // BASIC, LITE, PRO, ELITE
        public DateTime? PackageExpiresAt { get; set; } // Ngày hết hạn hiển thị bài đăng
    }
}