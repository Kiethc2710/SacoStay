namespace SacoStayAPI.Model.DTOs
{
    public class UpdateRoomPostStatusDTO
    {
        /// <summary>active | inactive (hoặc Active | Hidden)</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Số người đang ở trong căn (0 … MaxPeople).</summary>
        public int? CurrentPeople { get; set; }
    }
}
