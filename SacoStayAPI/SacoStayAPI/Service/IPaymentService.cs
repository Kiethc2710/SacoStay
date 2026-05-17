using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SacoStayAPI.Service
{
    public interface IPaymentService
    {
        Task<string> CreatePackagePaymentUrlAsync(Guid roomPostId, string packageName);
        Task HandleReturnAsync(IQueryCollection query);
    }
}