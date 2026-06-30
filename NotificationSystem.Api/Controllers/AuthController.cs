using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NotificationSystem.Api.Models.Dtos;
using NotificationSystem.Api.Models.Options;

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config, IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly IConfiguration _config = config;
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("login")]
    public ActionResult Login([FromBody] LoginDto dto)
    {
        // TODO: validar credenciales contra usuario real (DB / config / Identity)
        var validUser = dto.Username == _config["Auth:Username"] && dto.Password == _config["Auth:Password"];
        if (!validUser) return Unauthorized();

        // Generar token JWT
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, dto.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new { token = tokenString });
    }
}