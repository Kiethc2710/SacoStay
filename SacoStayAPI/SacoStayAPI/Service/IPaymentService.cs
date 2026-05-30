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
        Task<string> CreateTenantPackagePaymentUrlAsync(Guid userId, string packageName);
        Task HandleReturnAsync(IQueryCollection query);
        Task<string> BuildFrontendReturnUrlAsync(IQueryCollection query);
        Task HandleWebhookAsync(string payload);
        Task<IEnumerable<TransactionHistoryDTO>> GetTransactionHistoryAsync(Guid userId);
    }
}
