using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class CompileError : Exception
    {
        public enum ReturnCodeEnum
        {
            SourceDontExists,
            ResultAlreadyExists,
            BadCommand,
            BadArgs,
            WrongStart
        }

        public ReturnCodeEnum ReturnCode { get; }

        private List<string> Messages { get; }

        public CompileError(ReturnCodeEnum returnCode, string message) : base(message)
        {
            ReturnCode = returnCode;
            Messages = new List<string>();
            Messages.Add(message);
        }

        public void AddMessage(string message)
        {
            Messages.Add(message);
        }

        public string MessagesToString()
        {
            return string.Join("\n\t", Messages.ToArray());
        }

        public static void MinLength(int current, int minLength, string message)
        {
            if (current < minLength)
                throw new CompileError(ReturnCodeEnum.BadArgs, message);
        }

        public static void NotNull(object? ob, string message)
        {
            if (ob is null)
                throw new CompileError(ReturnCodeEnum.BadArgs, message);
        }
    }
}
