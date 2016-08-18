using System;
using System.CodeDom.Compiler;

namespace Cvent.SchemaToPoco.Core.CodeToLanguage
{
    public class PythonCodeDomProvider : CodeDomProvider, ICodeGenerator
    {
        private readonly ICodeGenerator _generator;
        public PythonCodeDomProvider()
        {
            _generator = new PythonCodeGenerator();;
        }
        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeGenerator CreateGenerator()
        {
            return _generator;
        }

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public override ICodeCompiler CreateCompiler()
        {
            throw new System.NotImplementedException();
        }

        public void ValidateIdentifier(string value)
        {
            throw new NotImplementedException();
        }
    }
}