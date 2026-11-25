using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MesServer.Models;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace MesServer.Services
{
    public class AuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<User> userManager,
                      IConfiguration configuration,
                      ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateADCredentials(string domain, string username, string password)
        {
            try
            {
                using var context = new PrincipalContext(ContextType.Domain, domain);
                return context.ValidateCredentials(username, password);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AD validation error");
                return false;
            }
        }
        //private readonly MemoryCache _adValidationCache = new MemoryCache(new MemoryCacheOptions());
        public async Task<string> GenerateJwtToken(User user)
        {
            // 1. Формируем claims 
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            };

            // 2. Получаем роли пользователя (если нужно)
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = roles.Select(role => new Claim(ClaimTypes.Role, role));
            claims = claims.Concat(roleClaims).ToArray();

            // 3. Создаем ключ и подпись
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 4. Создаем токен
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}