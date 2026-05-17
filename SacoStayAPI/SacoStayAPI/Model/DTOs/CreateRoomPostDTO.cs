using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace SacoStayAPI.Model.DTOs
{
    public class CreateRoomPostDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DetailedAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;

        public double Area { get; set; }
        public int MaxPeople { get; set; }
        public decimal Price { get; set; }

        public List<string> Amenities { get; set; } = new List<string>();

        // Khớp cấu trúc JSON phân cấp cho tọa độ bản đồ
        public LocationDTO Location { get; set; } = new LocationDTO();

        // Nhận tối đa 5 file ảnh phòng trọ tải lên từ giao diện kéo thả
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();
    }
}