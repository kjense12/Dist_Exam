using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Base.Extensions;

public static class IdentityExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) => GetUserId<Guid>(user);
    
    public static TKeyType GetUserId<TKeyType>(this ClaimsPrincipal user)
    {
        if (
            typeof(TKeyType) != typeof(Guid) &&
            typeof(TKeyType) != typeof(string) &&
            typeof(TKeyType) != typeof(int)
        )
        {
            throw new ApplicationException($"this type of User Id {typeof(TKeyType).Name} is not supporterd");
        }
        
        var idClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        if (idClaim == null)
        {
            throw new NullReferenceException("Name identifier claim not found");
        }

        var res = (TKeyType) TypeDescriptor.GetConverter(typeof(TKeyType)).ConvertFromInvariantString(idClaim.Value)!;

        return res;
    }

    public static string GetUserName(this ClaimsPrincipal user)
    {
        return 
            (user.Claims.FirstOrDefault(c => c.Type == "aspnet.firstname")?.Value ?? "???") + 
            " " + 
            (user.Claims.FirstOrDefault(c => c.Type == "aspnet.lastname")?.Value ?? "???");
    }

    public static string GenerateJwt(IEnumerable<Claim> claims, string key, string issuer, string audience, DateTime expirationDateTime)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expirationDateTime,
            signingCredentials: signingCredentials
            );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}