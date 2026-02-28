namespace SacoStayAPI.Service
{
    public interface IPaymentService
    {
        Task<string> CreatePayment(decimal amount);
        Task HandleReturnAsync(IQueryCollection query);

    }
}
