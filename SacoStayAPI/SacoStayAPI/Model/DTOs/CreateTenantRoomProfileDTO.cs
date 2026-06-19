using System.Collections.Generic;

namespace SacoStayAPI.Model.DTOs
{
    public class CreateTenantRoomProfileDTO
    {
        public string? City { get; set; }
        public string? District { get; set; }
        public int? MaxPeople { get; set; }
        public List<string>? Amenities { get; set; }
        public string? ExtraNotes { get; set; }
    }
}
