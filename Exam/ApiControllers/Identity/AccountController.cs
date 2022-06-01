using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using App.Domain.Identity;
using App.Public.DTO.v1;
using App.Public.DTO.v1.Identity;
using Base.Extensions;
using Exam.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Exam.ApiControllers.Identity;

[ApiVersion( "1.0" )]
[Route("api/v{version:apiVersion}/identity/[controller]/[action]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly Random _rnd = new Random();

        
    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IConfiguration configuration, ILogger<AccountController> logger, ApplicationDbContext context)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _configuration = configuration;
        _context = context;
    }
    
    /// <summary>
    /// Login into the rest backend - generates JWT to be included in
    /// Authorize: Bearer xyz
    /// </summary>
    /// <param name="loginDto">Supply email and password</param>
    /// <returns>JWT and refresh token</returns>
    [Produces( "application/json" )]
    [ProducesResponseType( typeof(JwtResponse), 200 )]
    [HttpPost]
    public async Task<ActionResult<JwtResponse>> LogIn([FromBody] Login loginDto)
    {
        // verify username
        var appUser = await _userManager.FindByEmailAsync(loginDto.Email);
        if (appUser == null)
        {
            _logger.LogWarning("WebApi login failed, email {} not found", loginDto.Email);
            await Task.Delay(_rnd.Next(100, 1000));
            return NotFound("User/Password problem");
        }
        // verify username and password
        var result = await _signInManager.CheckPasswordSignInAsync(appUser, loginDto.Password, false);
        if (!result.Succeeded)
        {
            _logger.LogWarning("WebApi login failed, password problem for user {}", loginDto.Email);
            await Task.Delay(_rnd.Next(100, 1000));
            return NotFound("User/Password problem");
        }
        // get claims based user
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        if (claimsPrincipal == null)
        {
            _logger.LogWarning("Could not get claimsPrincipal for user {}", loginDto.Email);
            await Task.Delay(_rnd.Next(100, 1000));
            return NotFound("User/Password problem");
        }
        // generate jwt
        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration["JWT:Key"],
            _configuration["JWT:Issuer"],
            _configuration["JWT:Issuer"],
            DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JWT:ExpireInMinutes"))
        );

        if (appUser.RefreshTokens == null)
        {
            var refreshToken = new RefreshToken();
            appUser.RefreshTokens = new List<RefreshToken>()
            {
                refreshToken
            };
        }
        else
        {
            var refreshToken = appUser.RefreshTokens.First();
            
            refreshToken.PreviousToken = refreshToken.Token;
            refreshToken.PreviousTokenExpirationDateTime = DateTime.UtcNow.AddMinutes(1);

            refreshToken.Token = Guid.NewGuid().ToString();
            refreshToken.ExpirationDateTime = DateTime.UtcNow.AddDays(7);
        }

        await _context.SaveChangesAsync();
        

        var res = new JwtResponse()
        {
            Token = jwt,
            RefreshToken = appUser.RefreshTokens.First().Token,
            RefreshTokenExpiration = appUser.RefreshTokens.First().ExpirationDateTime,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName
        };

        return Ok(res);
    }

    /// <summary>
    /// Register into the rest backend - generates JWT to be included in
    /// Authorize: Bearer xyz
    /// </summary>
    /// <param name="registrationData"> Supply Email, Password, FirstName, LastName</param>
    /// <returns>JWT and refresh token</returns>
    [ProducesResponseType( typeof(JwtResponse), 200 )]
    [HttpPost]
    public async Task<ActionResult<JwtResponse>> Register(Register registrationData)
    {
        // Verify user

        var appUser = await _userManager.FindByEmailAsync(registrationData.Email);
        if (appUser != null)
        {
            _logger.LogWarning("User with email {} already exists", registrationData.Email);
            var errorResponse = new RestApiErrorResponse(
            )
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "App error",
                Status = HttpStatusCode.BadRequest,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            };
            errorResponse.Errors["email"] = new List<string>()
            {
                "Email already registered"
            };
            return BadRequest(errorResponse);
        }

        var refreshToken = new RefreshToken();
        appUser = new AppUser()
        {
            FirstName = registrationData.FirstName,
            LastName = registrationData.LastName,
            Email = registrationData.Email,
            UserName = registrationData.Email,
            RefreshTokens = new List<RefreshToken>()
            {
                refreshToken
            }
        };
        // Create user
        var result = await _userManager.CreateAsync(appUser, registrationData.Password);

        if (!result.Succeeded)
        {
            return BadRequest(result);
        }
        
        result = await _userManager.AddClaimAsync(appUser, new Claim("aspnet.firstname", appUser.FirstName));
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }
        result = await _userManager.AddClaimAsync(appUser, new Claim("aspnet.lastname", appUser.LastName));
        // Get full user from db
        
        appUser = await _userManager.FindByEmailAsync(appUser.Email);
        
        if (appUser == null)
        {
            _logger.LogWarning("User with email {} is not found after registrations", registrationData.Email);
            return BadRequest($"User with email {registrationData.Email} is not found after registrations");
        }

        // get claims based user
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        if (claimsPrincipal == null)
        {
            _logger.LogWarning("Could not get claimsPrincipal for user {}", registrationData.Email);    
            return NotFound("User/Password problem");
        }
        // generate jwt
        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration["JWT:Key"],
            _configuration["JWT:Issuer"],
            _configuration["JWT:Issuer"],
            DateTime.Now.AddMinutes(_configuration.GetValue<int>("JWT:ExpireInMinutes"))
        );

        var res = new JwtResponse()
        {
            Token = jwt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.ExpirationDateTime,
            PreviousRefreshToken = refreshToken.PreviousToken,
            PreviousRefreshTokenExpiration = refreshToken.PreviousTokenExpirationDateTime,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName
        };

        return Ok(res);
    }

    /// <summary>
    /// Refresh JWT to authorize requests
    /// </summary>
    /// <param name="refreshTokenModel"> Supply valid JWT and valid refresh token to refresh JWT</param>
    /// <returns>JWT and refresh token</returns>
    [ProducesResponseType( typeof(JwtResponse), 200 )]
    [HttpPost]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenModel refreshTokenModel)
    {
        // Get user info from jwt
        JwtSecurityToken jwtToken;

        try
        {
            jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(refreshTokenModel.Jwt);
            if (jwtToken == null)
            {
                return BadRequest("No token");
            }
        }
        catch (Exception e)
        {
            return BadRequest($"Cant parse the token, {e.Message}");
        }
        
        // validate Token siganture

        var userEmail = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value;
        if (userEmail == null)
        {
            return BadRequest("No email");
        }
        // Get user 

        var appUser = await _userManager.FindByEmailAsync(userEmail);

        if (appUser == null)
        {
            return BadRequest($"User with email {userEmail} not found");
        }
        
        // Load and compare refresh tokens
        await _context.Entry(appUser).Collection(u => u.RefreshTokens!)
            .Query()
            .Where(x => 
                (x.Token == refreshTokenModel.RefreshToken && x.ExpirationDateTime > DateTime.UtcNow) || 
                (x.PreviousToken == refreshTokenModel.RefreshToken && x.PreviousTokenExpirationDateTime > DateTime.UtcNow))
            .ToListAsync();

        if (appUser.RefreshTokens == null)
        {
            return Problem("RefreshTokens collection is null");
        }
        
        if (appUser.RefreshTokens.Count == 0)
        {
            return Problem("RefreshTokens collection is empty, no valid refresh token found");
        }
        
        if (appUser.RefreshTokens.Count != 1)
        {
            return Problem("More than one valid refresh token found");
        }
        // Generate new jwt
        
        var claimsPrincipal = await _signInManager.CreateUserPrincipalAsync(appUser);
        if (claimsPrincipal == null)
        {
            _logger.LogWarning("Could not get claimsPrincipal for user {}", userEmail);    
            return NotFound("User/Password problem");
        }
        // generate jwt
        var jwt = IdentityExtensions.GenerateJwt(
            claimsPrincipal.Claims,
            _configuration["JWT:Key"],
            _configuration["JWT:Issuer"],
            _configuration["JWT:Issuer"],
            DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JWT:ExpireInMinutes"))
        );

        // Make new refresh token, obsolete old ones
        // Generate new refresh token
        // Save new refresh token, move old one to prev, update expiration 

        var refreshToken = appUser.RefreshTokens.First();
        if (refreshToken.Token == refreshTokenModel.RefreshToken)
        {
            refreshToken.PreviousToken = refreshToken.Token;
            refreshToken.PreviousTokenExpirationDateTime = DateTime.UtcNow.AddMinutes(1);

            refreshToken.Token = Guid.NewGuid().ToString();
            refreshToken.ExpirationDateTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();
        }

        var res = new JwtResponse()
        {
            Token = jwt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiration = refreshToken.ExpirationDateTime,
            PreviousRefreshToken = refreshToken.PreviousToken,
            PreviousRefreshTokenExpiration = refreshToken.PreviousTokenExpirationDateTime,
            FirstName = appUser.FirstName,
            LastName = appUser.LastName
        };

        return Ok(res);
    }
}