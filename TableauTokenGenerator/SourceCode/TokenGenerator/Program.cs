using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens;

namespace TokenGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string token = GenerateToken("tableau-admin", "0f13e02c-94d7-43a9-8fe8-092700d6a730", "2695b6ed-63cb-40ae-aec9-e427fbc457d6",
                "4z3VZAgdnzRqQKJYiGGi2nEaSbFiJcaeuoHTLZMkolU=");
            Console.WriteLine(token);
            
                Console.Read();

        }

        private static string GenerateToken(string Username, string ClientId, string Secret, string SecretValue)
        {

            var tokenHandler = new JwtSecurityTokenHandler();

            //secret value
            var key = Encoding.ASCII.GetBytes(SecretValue);

            var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
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
                SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key), System.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);

            //client id
            token.Header.Add("iss", ClientId);

            //secret id
            token.Header.Add("kid", Secret);

            return tokenHandler.WriteToken(token);

        }
    }
}
