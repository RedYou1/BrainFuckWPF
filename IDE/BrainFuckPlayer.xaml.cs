using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BrainFuck;

namespace IDE
{
    /// <summary>
    /// Logique d'interaction pour BrainFuckPlayer.xaml
    /// </summary>
    public partial class BrainFuckPlayer : Grid
    {
        private Interpreter Interpreter;

        private TextBlock[] values = new TextBlock[9];

        private string FilePath;

        private bool debug;

        public CancellationTokenSource cancellationToken = new();

        public int waitingTime;

        public BrainFuckPlayer(bool debug, string filePath)
        {
            this.debug = debug;
            FilePath = filePath;
            waitingTime = debug ? 10 : 0;

            InitializeComponent();

            update(false);

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new TextBlock() { TextAlignment = TextAlignment.Center };
                valuesPanel.Children.Add(values[i]);
            }

            Interpreter = new Interpreter(FilePath, addChar, input);

            refresh();


            Interpreter.BrainFuckBack.OnPtrChanged += refresh;
            Interpreter.BrainFuckBack.OnValueChanged += refresh;
        }

        private void addChar(char output)
            => this.output.Text += output;

        private byte[] input(int amount)
        {
            InputWindow inWin;
            do
            {
                inWin = new InputWindow(amount);
                inWin.ShowDialog();
            } while (!inWin.Finished);

            return Encoding.ASCII.GetBytes(inWin.Text);
        }

        private void refresh(object? sender = null, EventArgs? args = null)
        {
            int offset = Interpreter.BrainFuckBack.Ptr - (int)Math.Floor(values.Length / 2f);
            for (int i = 0; i < values.Length; i++)
            {
                short ptr = (short)(offset + i);
                if (ptr < 0)
                    ptr += BrainFuckBack.RANGE;
                if (ptr >= BrainFuckBack.RANGE)
                    ptr -= BrainFuckBack.RANGE;
                values[i].Text = $"ptr\n{ptr}\nvalue\n{Interpreter.BrainFuckBack[ptr]}";
                values[i].Background = Brushes.White;
            }
            values[(int)Math.Floor(values.Length / 2f)].Background = Brushes.Yellow;
        }


        private void update(bool playing)
        {
            btnPlay.IsEnabled = !playing;
            btnResume.IsEnabled = !playing;
            btnNext.IsEnabled = !playing;
            btnStop.IsEnabled = playing;
        }

        public bool IsPlaying => !cancellationToken.IsCancellationRequested && Interpreter.CurrentActionsPtr < Interpreter.CurrentActionsLength;

        public void Play(object? sender = null, RoutedEventArgs? args = null)
        {
            Interpreter = new Interpreter(FilePath, addChar, input);
            output.Text = "";

            refresh();

            Interpreter.BrainFuckBack.OnPtrChanged += refresh;
            Interpreter.BrainFuckBack.OnValueChanged += refresh;
            Resume();
        }

        public void Resume(object? sender = null, RoutedEventArgs? args = null)
        {
            cancellationToken = new();
            Task.Run(() =>
            {
                while (IsPlaying)
                {
                    Dispatcher.Invoke(() => Interpreter.Next());
                    Thread.Sleep(waitingTime);
                }
            });
            update(true);
        }

        public void Stop(object? sender = null, RoutedEventArgs? args = null)
        {
            cancellationToken.Cancel();
            update(false);
        }

        public void Next(object? sender = null, RoutedEventArgs? args = null)
        {
            if (Interpreter.CurrentActionsPtr < Interpreter.CurrentActionsLength)
                Interpreter.Next();
        }
    }
}
