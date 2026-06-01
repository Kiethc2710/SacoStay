using SacoStayAPI.Model.DTOs;
using SacoStayAPI.Model.Entities;

namespace SacoStayAPI.Service
{
    public interface IReportService
    {
        Task<bool> SubmitReportAsync(CreateReportRequest request);
        Task<IEnumerable<ReportResponseDTO>> GetListReportsAsync();
    }

}
