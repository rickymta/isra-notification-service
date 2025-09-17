using Microsoft.Extensions.Logging;
using NotificationService.Infrastructure.SignalR;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Text.Json;

namespace NotificationService.Infrastructure.SignalR;

/// <summary>
/// Redis-based connection manager for SignalR connections
/// </summary>
public class RedisConnectionManager : IConnectionManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisConnectionManager> _logger;
    
    private const string USER_CONNECTIONS_PREFIX = "signalr:user_connections:";
    private const string CONNECTED_USERS_KEY = "signalr:connected_users";
    private const string USER_GROUPS_PREFIX = "signalr:user_groups:";
    private const string GROUP_USERS_PREFIX = "signalr:group_users:";
    
    public RedisConnectionManager(IConnectionMultiplexer redis, ILogger<RedisConnectionManager> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        try
        {
            var userConnectionsKey = USER_CONNECTIONS_PREFIX + userId;
            
            // Add connection to user's connection set
            await _database.SetAddAsync(userConnectionsKey, connectionId);
            
            // Add user to connected users set
            await _database.SetAddAsync(CONNECTED_USERS_KEY, userId);
            
            // Set expiration for user connections (24 hours)
            await _database.KeyExpireAsync(userConnectionsKey, TimeSpan.FromHours(24));
            
            _logger.LogDebug("Added connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding connection {ConnectionId} for user {UserId}", connectionId, userId);
            throw;
        }
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        try
        {
            var userConnectionsKey = USER_CONNECTIONS_PREFIX + userId;
            
            // Remove connection from user's connection set
            await _database.SetRemoveAsync(userConnectionsKey, connectionId);
            
            // Check if user has any remaining connections
            var remainingConnections = await _database.SetLengthAsync(userConnectionsKey);
            
            if (remainingConnections == 0)
            {
                // Remove user from connected users set if no connections remain
                await _database.SetRemoveAsync(CONNECTED_USERS_KEY, userId);
                
                // Clean up user's groups
                await CleanupUserGroupsAsync(userId);
            }
            
            _logger.LogDebug("Removed connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing connection {ConnectionId} for user {UserId}", connectionId, userId);
            throw;
        }
    }

    public async Task<List<string>> GetUserConnectionsAsync(string userId)
    {
        try
        {
            var userConnectionsKey = USER_CONNECTIONS_PREFIX + userId;
            var connections = await _database.SetMembersAsync(userConnectionsKey);
            
            return connections.Select(c => c.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connections for user {UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        try
        {
            return await _database.SetContainsAsync(CONNECTED_USERS_KEY, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is online", userId);
            return false;
        }
    }

    public async Task<List<string>> GetConnectedUsersAsync()
    {
        try
        {
            var users = await _database.SetMembersAsync(CONNECTED_USERS_KEY);
            return users.Select(u => u.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connected users");
            return new List<string>();
        }
    }

    public async Task AddUserToGroupAsync(string userId, string groupName)
    {
        try
        {
            var userGroupsKey = USER_GROUPS_PREFIX + userId;
            var groupUsersKey = GROUP_USERS_PREFIX + groupName;
            
            // Add group to user's groups set
            await _database.SetAddAsync(userGroupsKey, groupName);
            
            // Add user to group's users set
            await _database.SetAddAsync(groupUsersKey, userId);
            
            // Set expiration
            await _database.KeyExpireAsync(userGroupsKey, TimeSpan.FromHours(24));
            await _database.KeyExpireAsync(groupUsersKey, TimeSpan.FromHours(24));
            
            _logger.LogDebug("Added user {UserId} to group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to group {GroupName}", userId, groupName);
            throw;
        }
    }

    public async Task RemoveUserFromGroupAsync(string userId, string groupName)
    {
        try
        {
            var userGroupsKey = USER_GROUPS_PREFIX + userId;
            var groupUsersKey = GROUP_USERS_PREFIX + groupName;
            
            // Remove group from user's groups set
            await _database.SetRemoveAsync(userGroupsKey, groupName);
            
            // Remove user from group's users set
            await _database.SetRemoveAsync(groupUsersKey, userId);
            
            _logger.LogDebug("Removed user {UserId} from group {GroupName}", userId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from group {GroupName}", userId, groupName);
            throw;
        }
    }

    public async Task<List<string>> GetGroupUsersAsync(string groupName)
    {
        try
        {
            var groupUsersKey = GROUP_USERS_PREFIX + groupName;
            var users = await _database.SetMembersAsync(groupUsersKey);
            
            return users.Select(u => u.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users for group {GroupName}", groupName);
            return new List<string>();
        }
    }

    public async Task<List<string>> GetUserGroupsAsync(string userId)
    {
        try
        {
            var userGroupsKey = USER_GROUPS_PREFIX + userId;
            var groups = await _database.SetMembersAsync(userGroupsKey);
            
            return groups.Select(g => g.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting groups for user {UserId}", userId);
            return new List<string>();
        }
    }

    private async Task CleanupUserGroupsAsync(string userId)
    {
        try
        {
            var userGroups = await GetUserGroupsAsync(userId);
            
            foreach (var group in userGroups)
            {
                await RemoveUserFromGroupAsync(userId, group);
            }
            
            // Clean up user's groups key
            var userGroupsKey = USER_GROUPS_PREFIX + userId;
            await _database.KeyDeleteAsync(userGroupsKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up groups for user {UserId}", userId);
        }
    }
}

/// <summary>
/// In-memory connection manager for SignalR connections (for development/testing)
/// </summary>
public class InMemoryConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _userConnections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userGroups = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _groupUsers = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryConnectionManager> _logger;

    public InMemoryConnectionManager(ILogger<InMemoryConnectionManager> logger)
    {
        _logger = logger;
    }

    public Task AddConnectionAsync(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_userConnections.ContainsKey(userId))
            {
                _userConnections[userId] = new HashSet<string>();
            }
            
            _userConnections[userId].Add(connectionId);
            
            _logger.LogDebug("Added connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (_userConnections.ContainsKey(userId))
            {
                _userConnections[userId].Remove(connectionId);
                
                if (_userConnections[userId].Count == 0)
                {
                    _userConnections.TryRemove(userId, out _);
                    
                    // Clean up user's groups
                    CleanupUserGroups(userId);
                }
            }
            
            _logger.LogDebug("Removed connection {ConnectionId} for user {UserId}", connectionId, userId);
        }
        
        return Task.CompletedTask;
    }

    public Task<List<string>> GetUserConnectionsAsync(string userId)
    {
        lock (_lock)
        {
            if (_userConnections.ContainsKey(userId))
            {
                return Task.FromResult(_userConnections[userId].ToList());
            }
            
            return Task.FromResult(new List<string>());
        }
    }

    public Task<bool> IsUserOnlineAsync(string userId)
    {
        lock (_lock)
        {
            return Task.FromResult(_userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0);
        }
    }

    public Task<List<string>> GetConnectedUsersAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_userConnections.Keys.ToList());
        }
    }

    public Task AddUserToGroupAsync(string userId, string groupName)
    {
        lock (_lock)
        {
            // Add group to user's groups
            if (!_userGroups.ContainsKey(userId))
            {
                _userGroups[userId] = new HashSet<string>();
            }
            _userGroups[userId].Add(groupName);
            
            // Add user to group's users
            if (!_groupUsers.ContainsKey(groupName))
            {
                _groupUsers[groupName] = new HashSet<string>();
            }
            _groupUsers[groupName].Add(userId);
            
            _logger.LogDebug("Added user {UserId} to group {GroupName}", userId, groupName);
        }
        
        return Task.CompletedTask;
    }

    public Task RemoveUserFromGroupAsync(string userId, string groupName)
    {
        lock (_lock)
        {
            // Remove group from user's groups
            if (_userGroups.ContainsKey(userId))
            {
                _userGroups[userId].Remove(groupName);
                
                if (_userGroups[userId].Count == 0)
                {
                    _userGroups.TryRemove(userId, out _);
                }
            }
            
            // Remove user from group's users
            if (_groupUsers.ContainsKey(groupName))
            {
                _groupUsers[groupName].Remove(userId);
                
                if (_groupUsers[groupName].Count == 0)
                {
                    _groupUsers.TryRemove(groupName, out _);
                }
            }
            
            _logger.LogDebug("Removed user {UserId} from group {GroupName}", userId, groupName);
        }
        
        return Task.CompletedTask;
    }

    public Task<List<string>> GetGroupUsersAsync(string groupName)
    {
        lock (_lock)
        {
            if (_groupUsers.ContainsKey(groupName))
            {
                return Task.FromResult(_groupUsers[groupName].ToList());
            }
            
            return Task.FromResult(new List<string>());
        }
    }

    public Task<List<string>> GetUserGroupsAsync(string userId)
    {
        lock (_lock)
        {
            if (_userGroups.ContainsKey(userId))
            {
                return Task.FromResult(_userGroups[userId].ToList());
            }
            
            return Task.FromResult(new List<string>());
        }
    }

    private void CleanupUserGroups(string userId)
    {
        if (_userGroups.ContainsKey(userId))
        {
            var userGroups = _userGroups[userId].ToList();
            
            foreach (var group in userGroups)
            {
                if (_groupUsers.ContainsKey(group))
                {
                    _groupUsers[group].Remove(userId);
                    
                    if (_groupUsers[group].Count == 0)
                    {
                        _groupUsers.TryRemove(group, out _);
                    }
                }
            }
            
            _userGroups.TryRemove(userId, out _);
        }
    }
}
