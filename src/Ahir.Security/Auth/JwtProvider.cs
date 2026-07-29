using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ahir.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace Ahir.Security.Auth;

public sealed class JwtProvider
{
    private readonly string _secret;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _tokenExpirationHours;
    private readonly int _refreshTokenExpirationDays;

    public JwtProvider(string secret, string issuer = "Ahir", string audience = "AhirAPI",
        int tokenExpirationHours = 24, int refreshTokenExpirationDays = 7)
    {
        _secret = secret ?? throw new ArgumentNullException(nameof(secret));
        _issuer = issuer;
        _audience = audience;
        _tokenExpirationHours = tokenExpirationHours;
        _refreshTokenExpirationDays = refreshTokenExpirationDays;
    }

    public AuthToken CreateToken(UserInfo user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("permissions", string.Join(",", user.Permissions))
        };

        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresAt = DateTime.UtcNow.AddHours(_tokenExpirationHours);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new AuthToken
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = GenerateRefreshToken(),
            ExpiresAt = expiresAt,
            TokenType = "Bearer",
            Permissions = user.Permissions
        };
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string RefreshToken(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
            throw new SecurityTokenException("Invalid refresh token.");
        return GenerateRefreshToken();
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}