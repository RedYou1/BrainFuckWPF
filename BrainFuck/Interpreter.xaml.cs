using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrainFuck
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Interpreter : Grid
    {
        private Action<Interpreter>[] actions;
        private TextBlock[] values = new TextBlock[9];
        private bool debug;
        private bool autoplay;

        public Interpreter(bool debug, bool autoplay, string filePath)
        {
            this.debug = debug;
            this.autoplay = autoplay;
            InitializeComponent();

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new TextBlock() { TextAlignment = TextAlignment.Center };
                valuesPanel.Children.Add(values[i]);
            }
            refresh();

            string file = File.ReadAllText(filePath);

            int ptr1 = 0;
            int ptr2 = 0;
            actions = parse(file, ref ptr1, ref ptr2);


            BrainFuckBack.OnPtrChanged += refresh;
            BrainFuckBack.OnValueChanged += refresh;
        }

        private void refresh(object? sender = null, EventArgs? args = null)
        {
            int offset = BrainFuckBack.Ptr - (int)Math.Floor(values.Length / 2f);
            for (int i = 0; i < values.Length; i++)
            {
                short ptr = (short)(offset + i);
                if (ptr < 0)
                    ptr += BrainFuckBack.RANGE;
                if (ptr >= BrainFuckBack.RANGE)
                    ptr -= BrainFuckBack.RANGE;
                values[i].Text = $"ptr\n{ptr}\nvalue\n{BrainFuckBack[ptr]}";
                values[i].Background = Brushes.White;
            }
            values[(int)Math.Floor(values.Length / 2f)].Background = Brushes.Yellow;
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
                            mainWindow.output.Text += (char)mainWindow.BrainFuckBack[mainWindow.BrainFuckBack.Ptr]);
                        actionPtr++;
                        break;
                    case ',':
                        {
                            int amount = amountInRow(actionsFile, ',', ref strPtr);
                            actions.Add((mainWindow) =>
                            {
                                InputWindow inWin;
                                do
                                {
                                    inWin = new InputWindow(amount);
                                    inWin.ShowDialog();
                                } while (!inWin.Finished);

                                byte[] value = Encoding.ASCII.GetBytes(inWin.Text);
                                mainWindow.BrainFuckBack.Set(value);
                            });
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

        public BrainFuckBack BrainFuckBack { get; } = new();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CurrentActionsLength = actions.Length;
            CurrentActionsPtr = 0;
            if (autoplay)
            {
                Task.Run(() =>
                {
                    for (; CurrentActionsPtr < CurrentActionsLength; CurrentActionsPtr++)
                    {
                        Dispatcher.Invoke(() =>
                            actions[CurrentActionsPtr].Invoke(this));
                        Thread.Sleep(debug ? 10 : 0);
                    }
                    Dispatcher.Invoke(() =>
                        Ended?.Invoke(this, new()));
                });
            }
        }

        public void Next()
        {
            actions[CurrentActionsPtr].Invoke(this);
            if (CurrentActionsPtr < CurrentActionsLength)
            {
                Ended?.Invoke(this, new());
            }
        }

        public event EventHandler Ended;

        public int CurrentActionsPtr { get; set; }
        public int CurrentActionsLength { get; set; }
    }
}
