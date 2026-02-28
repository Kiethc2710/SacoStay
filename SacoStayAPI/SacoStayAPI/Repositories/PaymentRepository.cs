using Microsoft.EntityFrameworkCore;
using SacoStayAPI.Data;
using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDBContext _context;

        public PaymentRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PaymentTransaction transaction)
        {
            await _context.PaymentTransactions.AddAsync(transaction);
            await _context.SaveChangesAsync();
        }

        public async Task<PaymentTransaction?> GetByOrderIdAsync(string orderId)
        {
            return await _context.PaymentTransactions
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task UpdateAsync(PaymentTransaction transaction)
        {
            _context.PaymentTransactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}
