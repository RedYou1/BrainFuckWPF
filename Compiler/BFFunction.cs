using static Compiler.Compiler;

namespace Compiler
{
    public class BFFunction
    {
        public BFFunction(int numberArgs, Action<CodeWriter, string[], bool> action)
        {
            NumberArgs = numberArgs;
            Action = action;
        }

        public int NumberArgs { get; }
        public Action<CodeWriter, string[], bool> Action { get; }
    }
}
