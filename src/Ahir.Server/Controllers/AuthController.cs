using System.Text.Json;
using Ahir.Core.Interfaces;
using Ahir.Core.Models;
using Ahir.Core.Utilities;
using Ahir.Security;
using Microsoft.AspNetCore.Mvc;

namespace Ahir.Server.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IServerHost _host;
    private const string UsersCollection = "_users";
    private const string UserDatabase = "_system";

    public AuthController(IServerHost host)
    {
        _host = host;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(AhirResult<object>.Fail("INVALID_INPUT", "Username and password are required."));

        if (request.Password.Length < 8)
            return BadRequest(AhirResult<object>.Fail("INVALID_INPUT", "Password must be at least 8 characters."));

        await EnsureSystemDatabaseAsync(ct);

        var existing = await ((ICollectionEngine)_host.Database).QueryAsync(UserDatabase, UsersCollection,
            new QueryOptions { Filters = new[] { new QueryFilter { Field = "username", Operator = FilterOperator.Equals, Value = request.Username } } }, ct);

        if (existing.Success && existing.Data != null && existing.Data.TotalCount > 0)
            return Conflict(AhirResult<object>.Fail("ALREADY_EXISTS", "Username already taken."));

        var hashResult = await _host.Security.HashPasswordAsync(request.Password, ct);
        if (!hashResult.Success)
            return BadRequest(hashResult);

        var roles = new List<string> { "viewer" };
        if (request.Username == "admin")
            roles.Add("admin");

        var fields = new Dictionary<string, object?>
        {
            ["username"] = request.Username,
            ["passwordHash"] = hashResult.Data ?? string.Empty,
            ["email"] = request.Email ?? string.Empty,
            ["roles"] = string.Join(",", roles),
            ["enabled"] = true,
            ["createdAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var insertResult = await ((ICollectionEngine)_host.Database).InsertAsync(UserDatabase, UsersCollection, fields, ct);
        if (!insertResult.Success)
            return BadRequest(insertResult);

        var userInfo = new UserInfo
        {
            Id = insertResult.Data?.Id ?? IdGenerator.NewId(),
            Username = request.Username,
            Email = request.Email ?? string.Empty,
            Roles = roles,
            Enabled = true,
            CreatedAt = DateTime.UtcNow
        };

        var tokenResult = await _host.Security.CreateTokenAsync(userInfo, ct);
        if (!tokenResult.Success)
            return BadRequest(tokenResult);

        return Created(string.Empty, new { user = userInfo, token = tokenResult.Data });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(AhirResult<object>.Fail("INVALID_INPUT", "Username and password are required."));

        await EnsureSystemDatabaseAsync(ct);

        var queryResult = await ((ICollectionEngine)_host.Database).QueryAsync(UserDatabase, UsersCollection,
            new QueryOptions { Filters = new[] { new QueryFilter { Field = "username", Operator = FilterOperator.Equals, Value = request.Username } } }, ct);

        if (!queryResult.Success || queryResult.Data == null || queryResult.Data.Items.Count == 0)
            return Unauthorized(AhirResult<object>.Fail("AUTH_FAILED", "Invalid username or password."));

        var userRecord = queryResult.Data.Items[0];
        var storedHash = GetField(userRecord, "passwordHash") ?? string.Empty;

        var verifyResult = await _host.Security.VerifyPasswordAsync(request.Password, storedHash, ct);
        if (!verifyResult.Success || !verifyResult.Data)
            return Unauthorized(AhirResult<object>.Fail("AUTH_FAILED", "Invalid username or password."));

        var rolesStr = GetField(userRecord, "roles") ?? string.Empty;
        var roles = rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        var permissions = roles.SelectMany(r => _host.Security is SecurityProvider sp ? sp.Permissions.GetPermissionsForRole(r) : Array.Empty<string>()).ToList();

        var userInfo = new UserInfo
        {
            Id = userRecord.Id,
            Username = request.Username,
            Email = GetField(userRecord, "email") ?? string.Empty,
            Roles = roles,
            Permissions = permissions,
            Enabled = true,
            CreatedAt = TryGetCreatedAt(userRecord),
            LastLoginAt = DateTime.UtcNow
        };

        var tokenResult = await _host.Security.CreateTokenAsync(userInfo, ct);
        if (!tokenResult.Success)
            return BadRequest(tokenResult);

        return Ok(new { user = userInfo, token = tokenResult.Data });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateRequest request, CancellationToken ct)
    {
        var result = await _host.Security.ValidateTokenAsync(request.Token, ct);
        return result.Success ? Ok(result.Data) : Unauthorized(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await _host.Security.RefreshTokenAsync(request.RefreshToken, ct);
        return result.Success ? Ok(result.Data) : Unauthorized(result);
    }

    private static string? GetField(AhirRecord record, string name)
    {
        if (record.Fields.TryGetValue(name, out var value))
            return value?.ToString();
        return null;
    }

    private static DateTime TryGetCreatedAt(AhirRecord record)
    {
        if (record.Fields.TryGetValue("createdAt", out var value) && value is long ms)
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        return DateTime.UtcNow;
    }

    private async Task EnsureSystemDatabaseAsync(CancellationToken ct)
    {
        var dbResult = await _host.Database.CreateAsync(UserDatabase, ct);
        if (!dbResult.Success && dbResult.ErrorCode != "ALREADY_EXISTS")
            throw new InvalidOperationException($"Failed to create system database: {dbResult.ErrorMessage}");

        await _host.Database.OpenAsync(UserDatabase, ct);
        var colResult = await ((ICollectionEngine)_host.Database).CreateAsync(UserDatabase, UsersCollection, ct);
        if (!colResult.Success && colResult.ErrorCode != "ALREADY_EXISTS")
            throw new InvalidOperationException($"Failed to create users collection: {colResult.ErrorMessage}");
    }
}

public sealed record RegisterRequest(string Username, string Password, string? Email = null);
public sealed record LoginRequest(string Username, string Password);
public sealed record ValidateRequest(string Token);
public sealed record RefreshRequest(string RefreshToken);
