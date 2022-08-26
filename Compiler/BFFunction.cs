using static Compiler.Compiler;

namespace Compiler
{
    public class BFFunction
    {
        public BFFunction(int numberArgs, Func<Compiler, string[], ReturnCode> action)
        {
            NumberArgs = numberArgs;
            Action = action;
        }

        public int NumberArgs { get; }
        public Func<Compiler, string[], ReturnCode> Action { get; }
    }
}
