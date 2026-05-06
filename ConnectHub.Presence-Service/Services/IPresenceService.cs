namespace ConnectHub.Presence.Services;

public interface IPresenceService
{
    Task UserConnected(int userId, string connectionId);
    Task UserDisconnected(int userId, string connectionId);
    Task<string[]> GetConnectionsByUserId(int userId);
    Task<int[]> GetOnlineUserIds();
    Task<bool> IsUserOnline(int userId);
    Task<int> GetConnectionCount();
    Task ClearUserConnections(int userId);
}