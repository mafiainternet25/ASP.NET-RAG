using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace web.Security;

public class JwtUtil
{
    private readonly IConfiguration _config;

    public JwtUtil(IConfiguration config)
    {
        _config = config;
    }

    private string GetSecret()
    {
        var secret = _config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT Secret not configured");
        return secret;
    }

    private int GetExpirationMinutes()
    {
        var expiration = _config.GetValue<int?>("Jwt:ExpirationMinutes") ?? 1440; // Default 24h
        return expiration;
    }

    private int GetRefreshExpirationDays()
    {
        var expiration = _config.GetValue<int?>("Jwt:RefreshExpirationDays") ?? 30;
        return expiration;
    }

    public string GenerateAccessToken(string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "QuocPhimApi",
            audience: _config["Jwt:Audience"] ?? "QuocPhimClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetExpirationMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken(string username)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim("type", "refresh")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "QuocPhimApi",
            audience: _config["Jwt:Audience"] ?? "QuocPhimClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(GetRefreshExpirationDays()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string? username, string? role) ValidateAccessToken(string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return (null, null);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret()));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "QuocPhimApi",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "QuocPhimClient",
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            var username = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            return (username, role);
        }
        catch
        {
            return (null, null);
        }
    }

    public string? ValidateRefreshToken(string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSecret()));
            var handler = new JwtSecurityTokenHandler();

            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"] ?? "QuocPhimApi",
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"] ?? "QuocPhimClient",
                ValidateLifetime = true
            }, out SecurityToken validatedToken);

            var tokenType = principal.FindFirst("type")?.Value;
            if (tokenType != "refresh")
                return null;

            return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }
}
