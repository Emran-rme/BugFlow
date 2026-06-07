using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BugFlow.Models;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    private string BuildToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateCompanyToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, "CompanyOwner")
        };

        return BuildToken(claims);
    }

    public string GenerateProjectToken(User user, int projectId, ProjectRole role)
    {
        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim("ProjectId", projectId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        return BuildToken(claims);
    }
}