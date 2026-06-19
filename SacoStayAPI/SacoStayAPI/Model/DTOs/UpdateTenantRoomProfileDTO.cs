using System.Collections.Generic;

namespace SacoStayAPI.Model.DTOs
{
    public class UpdateTenantRoomProfileDTO
    {
        public string? City { get; set; }
        public string? District { get; set; }
        public int? MaxPeople { get; set; }
        public List<string>? Amenities { get; set; }
        public string? ExtraNotes { get; set; }
        public decimal? Price { get; set; }
        public List<string>? Images { get; set; }
    }
}
