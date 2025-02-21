using BacNetExtension;
using BacNetExtension.CustomLists;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacNetExtensionTestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine();
            try
            {
                if (TryGetCustomList("BacNetObjectCustomList", out var customList))
                {
                    var data = new CustomListData
                    {
                        ListName = "List1",
                        Properties = { { "From", "111" }, { "To", "444" }, { "PWD", "blablalba...encoded" } }
                    };

                    if (TryCheckData(customList, data))
                    {
                        if (TryGetColumns(customList, data, out var columns))
                        {
                            if (TryGetItems(customList, data, out var items))
                            {
                                if (items == null || items.Count == 0)
                                {
                                    Console.WriteLine($"CustomList not returned any data.");
                                }
                                else
                                {
                                    Console.WriteLine($"CustomList returned items (first value: {items[0]["Value"]}).");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey();
            }
        }
        #region Helper Methods

        private static bool TryGetCustomList(string id, out ICustomList customList)
        {
            var extension = new BacNetExtensionDataSource(null);

            var resultDef = extension.GetCustomListDefinitions(); // Initialize system

            if (TryHandleResultOrIgnore(resultDef))
            {
                var result = extension.GetCustomList(id);

                customList = result.Value;

                return TryHandleResultOrIgnore(result);
            }

            customList = null;
            return false;
        }

        private static bool TryCheckData(ICustomList customList, CustomListData data)
        {
            var result = customList.CheckData(data);

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryGetColumns(ICustomList customList, CustomListData data, out CustomListColumnCollection columns)
        {
            var result = customList.GetColumns(data);

            columns = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryGetItems(ICustomList customList, CustomListData data, out CustomListObjectElementCollection items)
        {
            var result = customList.GetItems(data);

            items = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryHandleResultOrIgnore(Result result)
        {
            var hasErrors = result.HasErrors();
            var hasWarnings = result.HasWarnings();

            if (hasErrors || hasWarnings)
            {
                Console.Write($"CustomList execution ");

                if (hasErrors)
                {
                    Console.WriteLine("failed with error(s):");

                    Console.WriteLine(result.Errors.ToCombinedString(" => "));
                }

                if (result.HasWarnings())
                {
                    if (hasErrors)
                    {
                        Console.WriteLine("Additional warning(s):");
                    }
                    else
                    {
                        Console.WriteLine("succeeded with warning(s):");
                    }

                    Console.WriteLine(result.Warnings.ToCombinedString(" => "));
                }
            }

            return !hasErrors;
        }

        #endregion
    }
}
