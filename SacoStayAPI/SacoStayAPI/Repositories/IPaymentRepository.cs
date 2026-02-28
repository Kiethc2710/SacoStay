using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Repositories
{
    public interface IPaymentRepository
    {
        Task AddAsync(PaymentTransaction transaction);
        Task<PaymentTransaction?> GetByOrderIdAsync(string orderId);
        Task UpdateAsync(PaymentTransaction transaction);
    }
}
