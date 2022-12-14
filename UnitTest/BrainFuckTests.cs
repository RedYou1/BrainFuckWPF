using BrainFuck;
using Compiler;

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

            int numberIndex = 0;
            int byteIndex = 0;
            byte[] bytes = new byte[]
            {   1,
                2, 2,
                4, 4, 4, 4,

                5,
                4, 5, 4, 4,
                6, 5, 4, 3,
                4, 5, 4, 4};

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
        }

        [TestMethod]
        public void InOutString()
        {
            CompileError? result = Compile(nameof(InOutString));
            Assert.IsNull(result);

            Random rand = new Random(420);

            bool atLeastOne = false;
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
                    atLeastOne = true;
                    byte[] r = new byte[rand.Next(amount) + 1];
                    rand.NextBytes(r);
                    r = r.TakeWhile(b => b != 0).ToArray();
                    queue.Enqueue(r);
                    return r;
                });

            while (interpreter.CurrentActionsPtr < interpreter.CurrentActionsLength)
                interpreter.Next();

            Assert.IsTrue(atLeastOne);
            Assert.IsFalse(queue.Any());
        }

        class Person
        {
            public byte Age { get; private set; }
            public bool IsAlive { get; private set; }
            public byte Age2 => IsAlive ? (byte)(Age + 100) : Age;

            public Person(Random random)
            {
                Age = (byte)random.Next(0, byte.MaxValue);
                IsAlive = random.Next(0, 3) != 0 ? true : false; // 66%
            }
        }

        [TestMethod]
        public void ManipulateArray()
        {
            CompileError? result = Compile(nameof(ManipulateArray));
            Assert.IsNull(result);

            Random random = new Random(420);
            List<byte> Result = new();
            Person[] persons = new Person[100].Select(x => new Person(random)).ToArray();

            int i = 0;

            Interpreter interpreter = CreateInterpreter(nameof(ManipulateArray),
                (output) => Result.Add((byte)output),
                (amount) =>
                {
                    List<byte> bytes = new();
                    while (amount > 0)
                    {
                        if (i < persons.Length)
                        {
                            bytes.Add(persons[i].Age);
                            bytes.Add((byte)(persons[i].IsAlive ? 1 : 0));
                            i++;
                            amount -= 2;
                            continue;
                        }
                        else if (i == persons.Length)
                        {
                            bytes.Add(100);
                            i++;
                            amount--;
                            continue;
                        }
                        else
                        {
                            Assert.Fail();
                            return new byte[0];
                        }
                    }
                    return bytes.ToArray();
                });

            while (interpreter.CurrentActionsPtr < interpreter.CurrentActionsLength)
                interpreter.Next();

            Assert.AreEqual(persons.Length, Result.Count());
            for (int i2 = 0; i2 < persons.Length; i2++)
            {
                Assert.AreEqual(persons[i2].Age2, Result[i2]);
            }
        }
    }
}