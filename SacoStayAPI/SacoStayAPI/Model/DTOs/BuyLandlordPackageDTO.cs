namespace SacoStayAPI.Model.DTOs
{
    public class BuyLandlordPackageDTO
    {
        public Guid RoomPostId { get; set; }
        public string PackageName { get; set; } = string.Empty;
    }
}
