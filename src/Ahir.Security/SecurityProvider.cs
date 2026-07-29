using System.Security.Cryptography;
using Ahir.Core.Configuration;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Security.Auth;
using Ahir.Security.Encryption;

namespace Ahir.Security;

public sealed class SecurityProvider : ISecurityProvider
{
    private readonly Argon2IdEngine _argon2;
    private readonly JwtProvider _jwt;
    private readonly Aes256GcmEngine _aes;
    private readonly PermissionManager _permissions;
    private readonly SecurityConfig _config;

    public SecurityProvider(SecurityConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _argon2 = new Argon2IdEngine(config.ArgonMemorySize, config.ArgonIterations, config.ArgonParallelism);
        _jwt = new JwtProvider(
            config.JwtSecret,
            tokenExpirationHours: config.TokenExpirationHours,
            refreshTokenExpirationDays: config.RefreshTokenExpirationDays
        );
        _aes = new Aes256GcmEngine(Convert.FromHexString(config.JwtSecret[..Math.Min(config.JwtSecret.Length, 64)].PadRight(64, '0')[..32]));
        _permissions = new PermissionManager();
    }

    public Task<AhirResult<string>> HashPasswordAsync(string password, CancellationToken cancellationToken = default)
    {
        var hash = _argon2.HashPassword(password);
        return Task.FromResult(AhirResult<string>.Ok(hash));
    }

    public Task<AhirResult<bool>> VerifyPasswordAsync(string password, string hash, CancellationToken cancellationToken = default)
    {
        var valid = _argon2.VerifyPassword(password, hash);
        return Task.FromResult(AhirResult<bool>.Ok(valid));
    }

    public Task<AhirResult<AuthToken>> CreateTokenAsync(UserInfo user, CancellationToken cancellationToken = default)
    {
        var token = _jwt.CreateToken(user);
        return Task.FromResult(AhirResult<AuthToken>.Ok(token));
    }

    public Task<AhirResult<AuthToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var newRefresh = _jwt.RefreshToken(refreshToken);
            return Task.FromResult(AhirResult<AuthToken>.Ok(new AuthToken { RefreshToken = newRefresh }));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AhirResult<AuthToken>.Fail("TOKEN_INVALID", ex.Message));
        }
    }

    public Task<AhirResult<bool>> RevokeTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AhirResult<bool>.Ok(true));
    }

    public Task<AhirResult<UserInfo>> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        // In production: lookup user from database engine
        return Task.FromResult(AhirResult<UserInfo>.Fail("NOT_IMPLEMENTED", "Authentication will query the user store."));
    }

    public Task<AhirResult<UserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var principal = _jwt.ValidateToken(token);
        if (principal == null)
            return Task.FromResult(AhirResult<UserInfo>.Fail("TOKEN_INVALID", "Invalid or expired token."));

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? string.Empty;
        var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = principal.FindFirst("permissions")?.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new();

        return Task.FromResult(AhirResult<UserInfo>.Ok(new UserInfo
        {
            Id = userId,
            Username = username,
            Roles = roles,
            Permissions = permissions
        }));
    }

    public string GenerateApiKey()
    {
        var key = RandomNumberGenerator.GetBytes(32);
        return "ahir_" + Convert.ToHexStringLower(key);
    }

    public string GenerateSecret()
    {
        return Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(64));
    }

    public (byte[] Key, byte[] Iv) GenerateEncryptionKey()
    {
        return (Aes256GcmEngine.GenerateKey(), RandomNumberGenerator.GetBytes(12));
    }

    public PermissionManager Permissions => _permissions;
}

public sealed class PermissionManager
{
    private readonly Dictionary<string, HashSet<string>> _rolePermissions = new()
    {
        ["admin"] = new HashSet<string>
        {
            "server.*", "database.*", "collection.*", "record.*",
            "storage.*", "user.*", "plugin.*", "backup.*", "config.*", "monitor.*"
        },
        ["editor"] = new HashSet<string>
        {
            "database.read", "database.write", "collection.read", "collection.write",
            "record.*", "storage.read", "storage.write"
        },
        ["viewer"] = new HashSet<string>
        {
            "database.read", "collection.read", "record.read", "storage.read", "monitor.read"
        }
    };

    public bool HasPermission(string role, string requiredPermission)
    {
        if (!_rolePermissions.TryGetValue(role, out var permissions))
            return false;

        foreach (var perm in permissions)
        {
            if (MatchPermission(perm, requiredPermission))
                return true;
        }
        return false;
    }

    public bool HasPermission(IReadOnlyList<string> roles, string requiredPermission)
    {
        return roles.Any(role => HasPermission(role, requiredPermission));
    }

    public string[] GetPermissionsForRole(string role)
    {
        return _rolePermissions.TryGetValue(role, out var permissions)
            ? permissions.ToArray()
            : Array.Empty<string>();
    }

    public void AddRolePermission(string role, string permission)
    {
        if (!_rolePermissions.ContainsKey(role))
            _rolePermissions[role] = new HashSet<string>();
        _rolePermissions[role].Add(permission);
    }

    private static bool MatchPermission(string pattern, string required)
    {
        if (pattern.EndsWith(".*"))
            return required.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);
        return string.Equals(pattern, required, StringComparison.OrdinalIgnoreCase);
    }
}