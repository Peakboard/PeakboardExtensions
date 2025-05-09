using System;

namespace BacNetExtension.Helpers
{
    public static class StringHelper
    {
        public static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < parts.Length; i++)
            {
                var word = parts[i].ToLower();                
                parts[i] = char.ToUpper(word[0]) + word.Substring(1); 
            }

            return string.Concat(parts);
        }
    }
}