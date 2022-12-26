using static Compiler.Compiler;

namespace Compiler
{
    public class BFFunction
    {
        public BFFunction(int numberArgs, Action<Compiler, CodeWriter, string[], bool> action)
        {
            NumberArgs = numberArgs;
            Action = action;
        }

        public int NumberArgs { get; }
        public Action<Compiler, CodeWriter, string[], bool> Action { get; }
    }
}
