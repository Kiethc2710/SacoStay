using Microsoft.AspNetCore.Http;
using SacoStayAPI.Model.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public interface IPaymentService
    {
        Task<string> CreatePackagePaymentUrlAsync(Guid roomPostId, string packageName);
        Task HandleReturnAsync(IQueryCollection query);
        Task HandleWebhookAsync(string payload);
        Task<IEnumerable<TransactionHistoryDTO>> GetTransactionHistoryAsync(Guid userId);
    }
}
