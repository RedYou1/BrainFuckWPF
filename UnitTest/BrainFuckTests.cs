using BrainFuck;
using Compiler;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UnitTest
{
    [TestClass]
    public class BrainFuckTests
    {
        public BrainFuckTests()
        {
            Compiler.Compiler.Debug = false;
        }


        public CompileError? Compile(string method)
        {
            string path = $"../../tests/{method}/";
            return Compiler.Compiler.Compile(path, path + "starting", path + "build.bf");
        }

        public Interpreter CreateInterpreter(string method, Action<char> output, Func<int, byte[]> input)
            => new Interpreter($"../../tests/{method}/build.bf", output, input);


        [TestMethod]
        public void Numbers()
        {
            CompileError? result = Compile(nameof(Numbers));
            Assert.IsNull(result);

            dynamic[] numbers = new dynamic[]
            {
                (byte)0x01,
                (short)0x0202,
                0x04040404,
            };

            int numberIndex = 0;
            int byteIndex = 0;
            byte[] bytes;
            {
                List<byte> bytesList = new List<byte>() { (byte)numbers[0] };
                bytesList.AddRange(BitConverter.GetBytes((short)numbers[1]));
                bytesList.AddRange(BitConverter.GetBytes((int)numbers[2]));
                bytes = bytesList.ToArray();
            }

            Interpreter interpreter = CreateInterpreter(nameof(Numbers),
                (output) =>
                {
                    Assert.AreEqual(bytes[byteIndex++], (byte)output);
                },
                (amount) =>
                {
                    byte[] r = bytes.Skip(numberIndex).Take(amount).ToArray();
                    numberIndex += amount;
                    return r;
                });

            while (interpreter.CurrentActionsPtr < interpreter.CurrentActionsLength)
                interpreter.Next();

            Assert.AreEqual(bytes.Length, byteIndex);
            Assert.AreEqual(bytes.Length, numberIndex);
        }

        [TestMethod]
        public void InOutString()
        {
            CompileError? result = Compile(nameof(InOutString));
            Assert.IsNull(result);

            Random rand = new Random(420);

            int i = 0;
            Queue<byte[]> queue = new Queue<byte[]>();

            Interpreter interpreter = CreateInterpreter(nameof(InOutString),
                (output) =>
                {
                    byte[] bytes = queue.Peek();
                    Assert.AreEqual(bytes[i++], (byte)output);
                    if (i == bytes.Length)
                    {
                        queue.Dequeue();
                        i = 0;
                    }
                },
                (amount) =>
                {
                    byte[] r = new byte[rand.Next(amount) + 1];
                    rand.NextBytes(r);
                    queue.Enqueue(r);
                    return r;
                });

            while (interpreter.CurrentActionsPtr < interpreter.CurrentActionsLength)
                interpreter.Next();
        }
    }
}