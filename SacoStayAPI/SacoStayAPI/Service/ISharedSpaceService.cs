using SacoStayAPI.Model.DTOs;

namespace SacoStayAPI.Service
{
    public interface ISharedSpaceService
    {
        Task<object?> GetCurrentSpaceAsync(Guid userId);
        Task<(bool IsSuccess, string Message)> AddToShortlistAsync(Guid userId, Guid spaceId, AddToShortlistDTO dto);
        Task<(bool IsSuccess, string Message)> VoteRoomAsync(Guid userId, Guid shortlistId, VoteRoomDTO dto);
        //Task<(bool IsSuccess, string Message)> FinalizeSpaceAsync(Guid userId, Guid spaceId, FinalizeSpaceDTO dto);
        Task<(bool IsSuccess, string Message, Guid? SpaceId)> CreateSharedSpaceAsync(Guid user1Id, Guid user2Id);
        Task<(bool IsSuccess, string Message)> ProposeFinalizeAsync(Guid userId, Guid spaceId, FinalizeSpaceDTO dto);
        Task<(bool IsSuccess, string Message)> AcceptFinalizeAsync(Guid userId, Guid spaceId);
        Task<(bool IsSuccess, string Message)> RejectFinalizeAsync(Guid userId, Guid spaceId);
    }
}
