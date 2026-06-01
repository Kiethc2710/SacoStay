using SacoStayAPI.Model.DTOs;

namespace SacoStayAPI.Service
{
    public interface IReportService
    {
        Task<bool> SubmitReportAsync(CreateReportRequest request);
    }

}
