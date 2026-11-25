using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MesServer.Models;
using MesServer.Services;
using MesServer.Models.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MesServer.Data;
using Microsoft.EntityFrameworkCore;
using MesServer.Services;
using System.DirectoryServices.AccountManagement;

namespace MesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AuthController> _logger;
        private readonly AppDbContext _context;
        private readonly IContactRepository _contactRepository;
        private readonly IChatRepository _chatRepository;


        public AuthController(
    AuthService authService,
    UserManager<User> userManager,
    ILogger<AuthController> logger,
    AppDbContext context,
    IContactRepository contactRepository,
    IChatRepository chatRepository)
        {
            _authService = authService;
            _userManager = userManager;
            _logger = logger;
            _context = context;
            _contactRepository = contactRepository;
            _chatRepository = chatRepository;
        }
        [HttpPost("login/ad")]
        public async Task<IActionResult> LoginAD([FromBody] LoginDto model)
        {
            _logger.LogInformation("AuthController.LoginAD - Attempting AD login for user: {Username}", model.Username);

            try
            {
                // Проверка формата имени пользователя (должно быть DOMAIN\username)
                var domainParts = model.Username.Split('\\');
                if (domainParts.Length != 2)
                {
                    return BadRequest("Domain username should be in format 'DOMAIN\\username'");
                }

                var domain = domainParts[0];
                var username = domainParts[1];

                // Проверяем учетные данные в AD
                var authService = HttpContext.RequestServices.GetRequiredService<AuthService>();
                var isValid = await authService.ValidateADCredentials(domain, username, model.Password);

                if (!isValid)
                {
                    return Unauthorized("Invalid AD credentials");
                }

                // Поиск пользователя в базе данных
                var user = await _userManager.FindByNameAsync(model.Username);
                _logger.LogInformation("AuthController. Crz_002 {Username}", model.Username);
                if (user == null)
                {
                    _logger.LogWarning("AuthController.LoginAD - AD user not found: {Username}", model.Username);
                    return Unauthorized();
                }
                _logger.LogInformation("AuthController. Crz_004 {Username}", model.Username);
                //{
                // Генерация JWT токена
                _logger.LogInformation("AuthController. Crz_0041");
                    var token = await _authService.GenerateJwtToken(user);

                // Возврат токена и информации о пользователе
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsActiveDirectory = true
                    }
                });
                _logger.LogInformation("AuthController. Crz_020", model.Username);

                //var token = await _authService.GenerateJwtToken(user);
                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController. Error during AD login for user: {Username}", model.Username);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("login/windows")]
        public async Task<IActionResult> LoginWindowsIntegrated([FromBody] LoginWindowsDto model)
        {
            _logger.LogInformation("AuthController.LoginWindowsIntegrated - Attempting Windows integrated login for user: {Username}", model.Username);

            try
            {
                // Проверка формата имени пользователя (должно быть DOMAIN\username)
                var domainParts = model.Username.Split('\\');
                if (domainParts.Length != 2)
                {
                    return BadRequest("Domain username should be in format 'DOMAIN\\username'");
                }

                var domain = domainParts[0];
                var username = domainParts[1];

                //// Проверяем, что пользователь действительно вошел в систему Windows
                //if (!HttpContext.User.Identity.IsAuthenticated ||
                //    !HttpContext.User.Identity.Name.Equals(model.Username, StringComparison.OrdinalIgnoreCase))
                //{
                //    return Unauthorized("Windows authentication required");
                //}

                // Поиск пользователя в базе данных
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    _logger.LogWarning("AuthController.LoginWindowsIntegrated - Windows user not found: {Username}", model.Username);
                    return Unauthorized();
                }

                // Генерация JWT токена
                var token = await _authService.GenerateJwtToken(user);

                // Получение контактов и чатов пользователя
                var contacts = await _context.Contacts
                    .Where(c => c.UserId == user.Id)
                    .Include(c => c.ContactUser)
                    .Select(c => new
                    {
                        Id = c.ContactUserID,
                        Username = c.ContactUser.UserName,
                        Email = c.ContactUser.Email,
                        CustomDisplayName = c.CustomDisplayName
                    })
                    .ToListAsync();

                var chats = await _context.ChatParticipants
                    .Where(cp => cp.UserId == user.Id)
                    .Include(cp => cp.Chat)
                    .Select(cp => new
                    {
                        Id = cp.Chat.ChatID,
                        Name = cp.Chat.ChatName ?? "",
                        Type = cp.Chat.ChatType,
                        CreatedBy = cp.Chat.CreatedBy,
                        CreationDate = cp.Chat.CreationDate,
                        LastMessageDate = cp.Chat.LastMessageDate
                    })
                    .ToListAsync();

                // Возврат токена и данных пользователя
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsActiveDirectory = user.IsActiveDirectory
                    },
                    Contacts = contacts,
                    Chats = chats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController.LoginWindowsIntegrated Error during Windows integrated login for user: {Username}", model.Username);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("checkuser")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckUser([FromQuery] string username)
        {
            try
            {
                // Проверка существования пользователя и его типа (AD или локальный)
                var user = await _userManager.FindByNameAsync(username);

                if (user == null)
                {
                    return Ok(new { Exists = false });
                }

                return Ok(new
                {
                    Exists = true,
                    IsActiveDirectory = user.IsActiveDirectory
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user type");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            _logger.LogInformation("AuthController. Attempting login for user: {model.Username}", model.Username);
            _logger.LogInformation("Username {model.Username}, pass={model.Password}", model.Username, model.Password);
            try
            {
                // Поиск пользователя в базе данных
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {Username}", model.Username);
                    return Unauthorized();
                }
                _logger.LogInformation("User ID = {user.Id}", user.Id);

                // Проверка пароля
                if (!await _userManager.CheckPasswordAsync(user, model.Password))
                {
                    _logger.LogWarning("Invalid password for user: {model.Username}", model.Username);
                    return Unauthorized();
                }

                // Генерация JWT токена
                var token = await _authService.GenerateJwtToken(user);
                _logger.LogInformation("Successful login for user: {model.Username}", model.Username);
                

                // Получение контактов и чатов пользователя
                var contacts = await _context.Contacts
                    .Where(c => c.UserId == user.Id)
                    .Include(c => c.ContactUser)
                    .Select(c => new
                    {
                        Id = c.ContactUserID, 
                        Username = c.ContactUser.UserName,
                        Email = c.ContactUser.Email,
                        CustomDisplayName = c.CustomDisplayName
                    })
                    .ToListAsync();

                var chats = await _context.ChatParticipants
                    .Where(cp => cp.UserId == user.Id)
                    .Include(cp => cp.Chat)
                    .Select(cp => new
                    {
                        Id = cp.Chat.ChatID,
                        Name = cp.Chat.ChatName ?? "", // Обработка NULL
                        Type = cp.Chat.ChatType,
                        CreatedBy = cp.Chat.CreatedBy,
                        CreationDate = cp.Chat.CreationDate,
                        LastMessageDate = cp.Chat.LastMessageDate
                    })
                    .ToListAsync();
                _logger.LogInformation("AuthController. CrzS_1090");

                // Возврат токена и данных пользователя
                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IsActiveDirectory = user.IsActiveDirectory
                    },
                    Contacts = contacts.Select(c => new
                    {
                        Id = c.Id,
                        Username = c.Username,
                        Email = c.Email,
                        CustomDisplayName = c.CustomDisplayName
                    }),
                    Chats = chats.Select(c => new
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Type = c.Type
                    }).ToList()
                });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController. Error during login for user: {Username}", model.Username);
                return StatusCode(500, "AuthController. Internal server error");
            }
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            _logger.LogInformation("AuthController. Attempting to register new user: {Username}", model.Username);

            try
            {
                _logger.LogInformation("Username= {model.Username},Password={model.Password}", model.Username, model.Password);
                // Создание нового пользователя
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RegistrationDate = DateTime.UtcNow,
                    LastOnline = DateTime.UtcNow
                };

                // Сохранение пользователя в базе данных
                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("AuthController. User registration failed: {Errors}", string.Join(", ", result.Errors));
                    return BadRequest(result.Errors);
                }

                _logger.LogInformation("AuthController. User registered successfully: {Username}", model.Username);
                return Ok(new { Message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuthController. Error during user registration");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("register/ad")]
        public async Task<IActionResult> RegisterADUser([FromBody] RegisterADUserDto model)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByNameAsync(model.Username);
                if (existingUser != null)
                {
                    return BadRequest("User already exists");
                }

                // Verify AD credentials on server side too
                var domainParts = model.Username.Split('\\');
                if (domainParts.Length != 2)
                {
                    return BadRequest("Invalid username format");
                }

                var domain = domainParts[0];
                var username = domainParts[1];

                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    if (!context.ValidateCredentials(username, model.Password))
                    {
                        return Unauthorized("Invalid AD credentials");
                    }
                }

                // Create AD user
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RegistrationDate = DateTime.UtcNow,
                    LastOnline = DateTime.UtcNow,
                    IsActiveDirectory = true
                };

                // Create without password since we'll use AD auth
                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                return Ok(new { Message = "AD user registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering AD user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("register/windows")]
        public async Task<IActionResult> RegisterWindowsUser([FromBody] RegisterADUserDto model)
        {
            try
            {
                // Verify the request comes from the actual Windows user
                if (!HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.Identity.Name.Equals(model.Username, StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized("Windows authentication required");
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByNameAsync(model.Username);
                if (existingUser != null)
                {
                    return BadRequest("User already exists");
                }

                // Create Windows user without password
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RegistrationDate = DateTime.UtcNow,
                    LastOnline = DateTime.UtcNow,
                    IsActiveDirectory = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                return Ok(new { Message = "Windows user registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering Windows user");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}
