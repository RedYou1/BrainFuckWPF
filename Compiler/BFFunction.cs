namespace Compiler
{
    public interface BFFunction
    {
        public int NumberArgs { get; }
        public void Call(Compiler comp, params string[] args);
    }
}
