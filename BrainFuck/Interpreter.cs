using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BrainFuck
{
    public class Interpreter
    {
        private Action<Interpreter>[] actions;
        public BrainFuckBack BrainFuckBack { get; } = new();

        public int CurrentActionsPtr { get; private set; }
        public int CurrentActionsLength { get; private set; }

        public Action<char> PrintChar { get; set; }
        public Func<int, byte[]> Input { get; set; }

        public Interpreter(string filePath, Action<char> printChar, Func<int, byte[]> input)
        {
            PrintChar = printChar;
            Input = input;

            string file = File.ReadAllText(filePath);

            int ptr1 = 0;
            int ptr2 = 0;
            actions = parse(file, ref ptr1, ref ptr2);
            CurrentActionsLength = actions.Length;
            CurrentActionsPtr = 0;
        }

        public void Next()
        {
            if (CurrentActionsPtr >= CurrentActionsLength)
                throw new Exception("out of actions");
            actions[CurrentActionsPtr].Invoke(this);
            CurrentActionsPtr++;
        }

        private static Action<Interpreter>[] parse(string actionsFile, ref int strPtr, ref int actionPtr)
        {
            List<Action<Interpreter>> actions = new();
            for (; strPtr < actionsFile.Length; strPtr++)
            {
                string strAt = actionsFile.Substring(strPtr);
                if (strAt.Length >= 3 &&
                    strAt[0] == '[' &&
                    (strAt[1] == '-' || actionsFile[1] == '+') &&
                    strAt[2] == ']')
                {
                    actionPtr++;
                    actions.Add((mainWindow) =>
                        mainWindow.BrainFuckBack.Set(0));
                    strPtr += 2;
                    continue;
                }

                switch (actionsFile[strPtr])
                {
                    case '>':
                        {
                            actionPtr++;
                            short amount = (short)(amountInRow(actionsFile, '>', ref strPtr) % BrainFuckBack.RANGE);
                            if (amount != 0)
                                actions.Add((mainWindow) =>
                                    mainWindow.BrainFuckBack.Next(amount));
                            break;
                        }
                    case '<':
                        {
                            actionPtr++;
                            short amount = (short)(amountInRow(actionsFile, '<', ref strPtr) % BrainFuckBack.RANGE);
                            if (amount != 0)
                                actions.Add((mainWindow) =>
                                    mainWindow.BrainFuckBack.Prev(amount));
                            break;
                        }
                    case '+':
                        {
                            actionPtr++;
                            byte amount = (byte)(amountInRow(actionsFile, '+', ref strPtr) % byte.MaxValue);
                            if (amount != 0)
                                actions.Add((mainWindow) =>
                                    mainWindow.BrainFuckBack.Add(amount));
                            break;
                        }
                    case '-':
                        {
                            actionPtr++;
                            byte amount = (byte)(amountInRow(actionsFile, '-', ref strPtr) % byte.MaxValue);
                            if (amount != 0)
                                actions.Add((mainWindow) =>
                                    mainWindow.BrainFuckBack.Sub(amount));
                            break;
                        }
                    case '.':
                        actions.Add((mainWindow) =>
                            mainWindow.PrintChar((char)mainWindow.BrainFuckBack[mainWindow.BrainFuckBack.Ptr]));
                        actionPtr++;
                        break;
                    case ',':
                        {
                            int amount = amountInRow(actionsFile, ',', ref strPtr);
                            actions.Add((mainWindow) =>
                            mainWindow.BrainFuckBack.Set(mainWindow.Input(amount)));
                            actionPtr++;
                            break;
                        }
                    case '[':
                        strPtr++;
                        int start = actionPtr - 1;
                        actionPtr++;
                        Action<Interpreter>[] innerActions = parse(actionsFile, ref strPtr, ref actionPtr);
                        int end = actionPtr;
                        actions.Add((mainWindow) =>
                        {
                            if (mainWindow.BrainFuckBack[mainWindow.BrainFuckBack.Ptr] == 0)
                                mainWindow.CurrentActionsPtr = end;
                        });
                        actions.AddRange(innerActions);
                        actions.Add((mainWindow) =>
                        {
                            mainWindow.CurrentActionsPtr = start;
                        });
                        actionPtr++;
                        break;
                    case ']':
                        return actions.ToArray();
                }
            }
            return actions.ToArray();
        }

        private static int amountInRow(string actionsFile, char command, ref int strPtr)
        {
            int result = 1;
            while (actionsFile.Length > strPtr + 1)
            {
                char next = actionsFile[strPtr + 1];
                if (next == '<' || next == '>' ||
                    next == '+' || next == '-' ||
                    next == '.' || next == ',' ||
                    next == '[' || next == ']')
                {
                    if (next == command)
                    {
                        result++;
                    }
                    else
                    {
                        return result;
                    }
                }
                strPtr++;
            }
            return result;
        }
    }
}
