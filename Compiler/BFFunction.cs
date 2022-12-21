using static Compiler.Compiler;

namespace Compiler
{
    public class BFFunction
    {
        public BFFunction(int numberArgs, Func<Compiler, CodeWriter, string[], bool, ReturnCode> action)
        {
            NumberArgs = numberArgs;
            Action = action;
        }

        public int NumberArgs { get; }
        public Func<Compiler, CodeWriter, string[], bool, ReturnCode> Action { get; }
    }
}
