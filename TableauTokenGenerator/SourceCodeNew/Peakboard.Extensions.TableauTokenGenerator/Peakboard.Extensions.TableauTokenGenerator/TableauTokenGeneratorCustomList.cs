using Microsoft.IdentityModel.Tokens;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Peakboard.Extensions.TableauTokenGenerator
{
    [CustomListIcon("Peakboard.Extensions.TableauTokenGenerator.logo.png")]
    internal class TableauTokenGeneratorCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "TableauTokenGeneratorCustomList",
                Name = "TableauTokenGenerator",
                Description = "Gets a token",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Username", Value = "tableau-admin" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "0f13e02c-94d7-43a9-8fe8-092700d6a730" },
                    new CustomListPropertyDefinition() { Name = "Secret", Value = "2695b6ed-63cb-40ae-aec9-e427fbc457d6" },
                    new CustomListPropertyDefinition() { Name = "SecretValue", Value = "4z3VZAgdnzRqQKJYiGGi2nEaSbFiJcaeuoHTLZMkolU=" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("Token", CustomListColumnTypes.String),
            };

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string Token = GenerateToken(data.Properties["Username"], data.Properties["ClientId"], data.Properties["Secret"], data.Properties["SecretValue"]);
            var items = new CustomListObjectElementCollection();

            CustomListObjectElement newitem = new CustomListObjectElement
                {
                    { "Token", Token }
                };
            items.Add(newitem);

            return items;
        }



        protected override void CheckDataOverride(CustomListData data)
        {
            bool validData =
                data.Properties.TryGetValue("Username", out var Username) &&
                data.Properties.TryGetValue("ClientId", out var ClientId) &&
                data.Properties.TryGetValue("Secret", out var Secret) &&
                data.Properties.TryGetValue("SecretValue", out var SecretValue) &&
                !string.IsNullOrEmpty(Username) &&
                !string.IsNullOrEmpty(ClientId) &&
                !string.IsNullOrEmpty(Secret) &&
                !string.IsNullOrEmpty(SecretValue);

            if (!validData)
            {
                throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties.");
            }

            base.CheckDataOverride(data);
        }


        private static string GenerateToken(string Username, string ClientId, string Secret, string SecretValue)
        {

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

        }
    }
}