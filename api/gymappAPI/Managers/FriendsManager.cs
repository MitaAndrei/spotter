using gymappAPI.Data;
using gymappAPI.DTOs;
using gymappAPI.Models;
using gymappAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace gymappAPI.Managers;

public class FriendsManager : IFriendsManager
{
    private readonly AppDbContext _context;
    private readonly IUserManager _userManager;
    private readonly IS3Service _s3Service;

    public FriendsManager(AppDbContext context, IUserManager userManager, IS3Service s3Service)
    {
        _context = context;
        _userManager = userManager;
        _s3Service = s3Service;
    }
    
    public async Task<bool> SendRequestAsync(Guid receiverId)
    {
        var sender = await _userManager.GetCurrentUserAsync();
        var senderId = sender.Id;
        
        if (senderId == receiverId) return false; 
        
        bool exists = await _context.FriendRequests.AnyAsync(fr =>
            (fr.SenderId == senderId && fr.ReceiverId == receiverId) ||
            (fr.SenderId == receiverId && fr.ReceiverId == senderId));
        
        if (exists) return false;

        var request = new FriendRequest
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Accepted = false
        };
        
        _context.FriendRequests.Add(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptRequestAsync(Guid otherUserId)
    {
        var currentUser = await _userManager.GetCurrentUserAsync();
        var currentUserId = currentUser.Id;
        
        if(currentUserId == otherUserId) return false;
        
        var request = await _context.FriendRequests.FirstOrDefaultAsync(fr =>
            fr.SenderId == otherUserId && fr.ReceiverId == currentUserId);

        if (request is null || request.Accepted) return false;
        
        request.Accepted = true;
        await _context.SaveChangesAsync();
        return true;

    }

    public async Task<bool> RejectRequestAsync(Guid otherUserId)
    {
        var currentUser = await _userManager.GetCurrentUserAsync();
        var currentUserId = currentUser.Id;
        
        if(currentUserId == otherUserId) return false;
        
        var request = await _context.FriendRequests.FirstOrDefaultAsync(fr =>
            fr.SenderId == otherUserId && fr.ReceiverId == currentUserId);

        if (request is null || request.Accepted) return false;

        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;

    }

    public async Task<bool> UnfriendAsync(Guid otherUserId)
    {
        var currentUser = await _userManager.GetCurrentUserAsync();
        var currentUserId = currentUser.Id;
        
        if(currentUserId == otherUserId) return false;
        
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr =>
                (fr.SenderId == currentUserId && fr.ReceiverId == otherUserId) ||
                (fr.SenderId == otherUserId && fr.ReceiverId == currentUserId));
        

        if (request is null || !request.Accepted) return false;
        
        _context.FriendRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FriendshipStatus> GetFriendshipStatusAsync(Guid otherUserId)
    {
        var currentUser = await _userManager.GetCurrentUserAsync();
        var currentUserId = currentUser.Id;
        
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr =>
                (fr.SenderId == currentUserId && fr.ReceiverId == otherUserId) ||
                (fr.SenderId == otherUserId && fr.ReceiverId == currentUserId));

        if (request == null)
            return FriendshipStatus.None;

        if (request.Accepted)
            return FriendshipStatus.Friends;

        if (request.SenderId == currentUserId)
            return FriendshipStatus.PendingSent;

        return FriendshipStatus.PendingReceived;
    }
    
    public async Task<bool> CanViewActivityOfAsync(Guid userId)
    {
        var currentUser = await _userManager.GetCurrentUserAsync();
        if (currentUser.Id == userId) return true;

        return await GetFriendshipStatusAsync(userId) == FriendshipStatus.Friends;
    }

    public async Task<UserDto[]> GetFriendsAsync(Guid userId)
    {
        var friends = await _context.FriendRequests
            .Where(fr => (fr.ReceiverId == userId || fr.SenderId == userId) && fr.Accepted)
            .Select(fr => new UserDto
            {
                Id = (fr.SenderId == userId ? fr.Receiver.Id : fr.Sender.Id),
                Username = (fr.SenderId == userId ? fr.Receiver.Username : fr.Sender.Username),
                ProfileImageKey = (fr.SenderId == userId ? fr.Receiver.ProfileImageKey : fr.Sender.ProfileImageKey),
                ProfileImageUrl = null
            })
            .ToArrayAsync();
        
        var urlTasks = friends
            .Where(f => !string.IsNullOrEmpty(f.ProfileImageKey))
            .Select(async friend =>
            {
                friend.ProfileImageUrl = await _s3Service.GetPreSignedUrlAsync(friend.ProfileImageKey!);
            });

        await Task.WhenAll(urlTasks);
        
        return friends;
    }
}