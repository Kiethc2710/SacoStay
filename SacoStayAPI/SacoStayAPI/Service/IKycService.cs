using SacoStayAPI.Model.DTOs;

namespace SacoStayAPI.Service
{
    public interface IKycService
    {
        Task<(bool IsSuccess, string Message)> SubmitKycAsync(Guid userId, SubmitKycRequestDTO dto);
        Task<object?> GetUserKycStatusAsync(Guid userId);
    }
}
