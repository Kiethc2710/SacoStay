namespace SacoStayAPI.Model.DTOs
{
    public class UpdateRoomPostStatusDTO
    {
        /// <summary>active | inactive (hoặc Active | Hidden)</summary>
        public string Status { get; set; } = string.Empty;
    }
}
