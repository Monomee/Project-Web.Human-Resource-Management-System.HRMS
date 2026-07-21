using System;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Threading.Tasks;

namespace HRMS.WebUI.Services;

public class TempTokenStore
{
    private readonly ConcurrentDictionary<string, ClaimsPrincipal> _store = new();

    public string GenerateToken(ClaimsPrincipal principal)
    {
        var token = Guid.NewGuid().ToString("N");
        _store[token] = principal;
        
        // Tự động xóa token sau 30 giây để tránh rò rỉ bộ nhớ nếu người dùng không redirect
        Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t => _store.TryRemove(token, out _));
        
        return token;
    }

    public ClaimsPrincipal? GetAndRemove(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        _store.TryRemove(token, out var principal);
        return principal;
    }

    public ClaimsPrincipal? Get(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        _store.TryGetValue(token, out var principal);
        return principal;
    }
}
