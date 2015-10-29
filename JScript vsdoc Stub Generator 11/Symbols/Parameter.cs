using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JScript_vsdoc_Stub_Generator_11.Symbols
{
    /// <summary>
    /// Represents a JavaScript or TypeScript function parameter
    /// </summary>
    public class Parameter
    {
        private Parameter() { }

        public bool IsTypeKnown
        {
            get
            {
                return !String.IsNullOrEmpty(Type);
            }
        }

        public string Type { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// Parses a string representing a single parameter.
        /// </summary>
        /// <param name="parameterString"></param>
        /// <returns></returns>
        public static Parameter Parse(string parameterString)
        {
            var result = new Parameter();
            parameterString = parameterString.Trim();

            if (!parameterString.Contains(':'))
            {
                result.Name = parameterString;
                return result;
            }

            var paramParts = parameterString.Split(':').Select(p => p.Trim()).ToArray();
            result.Name = paramParts[0];

            var typeString = paramParts[1];
            if (typeString.Contains("=>"))
            {
                result.Type = "function";
            }
            else
            {
                result.Type = typeString;
            }

            return result;
        }
    }
}
