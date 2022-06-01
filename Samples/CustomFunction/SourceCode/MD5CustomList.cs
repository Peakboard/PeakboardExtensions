using System;
using System.Security.Cryptography;
using System.Text;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionMD5
{
    [Serializable]
    class MD5CustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"dummylist",
                Name = "Dummy",
                Description = "Nothing",
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "getmd5hash",
                        Description = "Get Md5 Hash",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "input",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false
                            },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "hash",
                                Type = CustomListFunctionParameterTypes.String
                            }
                        }
                    }
                }
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var returnContext = new CustomListExecuteReturnContext();

            if (context.FunctionName.Equals("getmd5hash", StringComparison.InvariantCultureIgnoreCase))
            {
                returnContext.Add(GetMd5Hash(context.Values[0].ToString()));
            }

            return returnContext;
        }

        static string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute hash
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a Stringbuilder object to collect the bytes and create a string
                var sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data and format it as hex string
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                return sBuilder.ToString();
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Dummy", CustomListColumnTypes.Number),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement { { "Dummy", 0}});
            return items;
        }
    }
}