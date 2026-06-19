using System;
using System.Collections.Generic;

namespace SacoStayAPI.Model.DTOs
{
    public class TenantRoomProfileDTO
    {
        public Guid UserId { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public int? MaxPeople { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
        public string? ExtraNotes { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public DateTime UpdatedAt { get; set; }
    }
}
