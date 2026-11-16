using gymappAPI.DTOs;
using gymappAPI.Models;

namespace gymappAPI.Managers;

public interface IFriendsManager
{
    Task<bool> SendRequestAsync(Guid receiverId);
    Task<bool> AcceptRequestAsync(Guid otherUserId);
    Task<bool> RejectRequestAsync(Guid otherUserId);
    Task<bool> UnfriendAsync(Guid otherUserId);
    Task<FriendshipStatus> GetFriendshipStatusAsync(Guid otherUserId);

    // Workouts and logged foods are visible to their owner and the owner's friends.
    Task<bool> CanViewActivityOfAsync(Guid userId);
    
    Task<UserDto[]> GetFriendsAsync(Guid userId);
    
}