namespace Compiler.BFFunctions
{
    public class BFFPrint : BFFunction
    {
        public int NumberArgs => 1;

        public void Call(Compiler comp, params string[] args)
        {
            foreach (string arg in args)
            {
                comp.Move(comp.Memory[arg]);
                comp.StreamWriter.Write('.');
            }
        }
    }
}
