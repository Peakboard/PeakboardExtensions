// See https://aka.ms/new-console-template for more information
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;

Console.WriteLine("Hello, World!");



var tokenHandler = new JwtSecurityTokenHandler();

//secret value
var key = Encoding.ASCII.GetBytes(SecretValue);

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(new[] {
            new Claim("sub",Username)
            ,new Claim("aud","tableau")
            ,new Claim("jti",DateTime.UtcNow.ToString("MM/dd/yyyy hh:mm:ss.fff tt"))
            ,new Claim("iss",ClientId)
            ,new Claim("scp","tableau:views:embed")
            ,new Claim("scp"," ")
        }),
    Expires = DateTime.UtcNow.AddMinutes(2),
    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
};

var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);

//client id
token.Header.Add("iss", ClientId);

//secret id
token.Header.Add("kid", Secret);

return tokenHandler.WriteToken(token);