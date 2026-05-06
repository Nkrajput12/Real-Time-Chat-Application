using System.Collections.Concurrent;
namespace ConnectHub.Presence.Services;

public class PresenceService : IPresenceService
{

    private static readonly ConcurrentDictionary<int, HashSet<string>> OnlineUsers = new();
    public Task UserConnected(int userId,string connectionId)
    {

        lock(OnlineUsers)
        {
            if(!OnlineUsers.ContainsKey(userId))
            {
                OnlineUsers[userId] = new HashSet<string>();
            }
            OnlineUsers[userId].Add(connectionId);
        }
        return Task.CompletedTask;
    }

    public Task UserDisconnected(int userId, string connectionId)
    {
        lock (OnlineUsers)
        {
            if (!OnlineUsers.ContainsKey(userId)) return Task.CompletedTask;

            OnlineUsers[userId].Remove(connectionId);

            if (OnlineUsers[userId].Count == 0)
            {
                OnlineUsers.TryRemove(userId, out _);
            }
        }
        return Task.CompletedTask;
    }

    public Task<string[]> GetConnectionsByUserId(int userId)
    {
        return Task.FromResult(OnlineUsers.GetValueOrDefault(userId)?.ToArray() ?? Array.Empty<string>());
    }

    public Task<int[]> GetOnlineUserIds()
    {
        return Task.FromResult(OnlineUsers.Keys.ToArray());
    }

    public Task<bool> IsUserOnline(int userId)
    {
        return Task.FromResult(OnlineUsers.ContainsKey(userId));
    }

    public Task<int> GetConnectionCount()
    {
        return Task.FromResult(OnlineUsers.Sum(x => x.Value.Count));
    }

    public Task ClearUserConnections(int userId)
    {
        OnlineUsers.TryRemove(userId, out _);
        return Task.CompletedTask;
    }
}