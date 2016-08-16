using System.CodeDom;
using System.Text;
using Cvent.SchemaToPoco.Core.Wrappers;
using Microsoft.CSharp;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    /// <summary>
    ///     Compile a CodeCompileUnit to C# code.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class CodeCompileUnitToPython : CodeCompileUnitToLanguageBase
    {
        private const string PHP_NAME_SPACE_DELIMITER = "\\";
        private readonly CodeCompileUnit _codeCompileUnit;

        public CodeCompileUnitToPython(CodeCompileUnit codeCompileUnit)
            : base(codeCompileUnit)
        {
            _codeCompileUnit = codeCompileUnit;
        }

        /// <summary>
        ///     Main executor function.
        /// </summary>
        /// <returns>A string of generated PHP code.</returns>
        public override string Execute()
        {
            
            using (var codeProvider = new PythonCodeDomProvider())
            {
                return CodeUnitToLanguage(codeProvider);
            }
        }
    }
}
