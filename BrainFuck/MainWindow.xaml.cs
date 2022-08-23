using System;
using System.Collections.Generic;
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
    public partial class MainWindow : Window
    {
        private Action<MainWindow>[] actions;
        private TextBlock[] values = new TextBlock[9];

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new TextBlock() { TextAlignment = TextAlignment.Center };
                valuesPanel.Children.Add(values[i]);
            }
            refresh();

            string file;
#pragma warning disable CS0162 // Code inaccessible détecté
            switch (6)
            {
                case 0:
                    file = ReadAllText("HelloWorld.txt");
                    break;
                case 1:
                    file = ReadAllText("AroundTheWorld.txt");
                    break;
                case 2:
                    file = ReadAllText("overflow.txt");
                    break;
                case 3:
                    file = ReadAllText("overflow2.txt");
                    break;
                case 4:
                    file = ReadAllText("cat.txt");
                    break;
                case 5:
                    file = ReadAllText("cat2.txt");
                    break;
                case 6:
                    file = ReadAllText("IDE.bf");
                    break;
            }
#pragma warning restore CS0162 // Code inaccessible détecté

            int ptr1 = 0;
            int ptr2 = 0;
            actions = parse(file, ref ptr1, ref ptr2);


            BrainFuckBack.OnPtrChanged += refresh;
            BrainFuckBack.OnValueChanged += refresh;
        }

        private static string ReadAllText(string name)
        {
            return File.ReadAllText($@"..\..\..\tests\{name}");
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

        private static Action<MainWindow>[] parse(string actionsFile, ref int strPtr, ref int actionPtr)
        {
            List<Action<MainWindow>> actions = new();
            for (; strPtr < actionsFile.Length; strPtr++)
            {
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
                        Action<MainWindow>[] innerActions = parse(actionsFile, ref strPtr, ref actionPtr);
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
            Task.Run(() =>
            {
                CurrentActionsLength = actions.Length;
                for (CurrentActionsPtr = 0; CurrentActionsPtr < CurrentActionsLength; CurrentActionsPtr++)
                {
                    Dispatcher.Invoke(() =>
                        actions[CurrentActionsPtr].Invoke(this));
                    Thread.Sleep(10);
                }
                MessageBox.Show("ended");
            });
        }

        public int CurrentActionsPtr { get; set; }
        public int CurrentActionsLength { get; set; }
    }
}
