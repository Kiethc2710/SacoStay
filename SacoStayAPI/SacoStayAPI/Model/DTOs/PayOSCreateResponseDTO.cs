namespace SacoStayAPI.Model.DTOs
{
    public class PayOSCreateResponseDTO
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public PayOSCreateDataDTO? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PayOSCreateDataDTO
    {
        public string? Id { get; set; }
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string? Status { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
