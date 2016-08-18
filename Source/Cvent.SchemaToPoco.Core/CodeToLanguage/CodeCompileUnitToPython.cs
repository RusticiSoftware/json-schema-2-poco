using System.CodeDom;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    /// <summary>
    ///     Compile a CodeCompileUnit to C# code.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class CodeCompileUnitToPython : CodeCompileUnitToLanguageBase
    {

        public CodeCompileUnitToPython(CodeCompileUnit codeCompileUnit)
            : base(codeCompileUnit)
        {
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
