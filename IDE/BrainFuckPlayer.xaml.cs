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

        private bool debug;
        private bool autoplay;

        public BrainFuckPlayer(bool debug, bool autoplay, string filePath)
        {
            this.debug = debug;
            this.autoplay = autoplay;

            InitializeComponent();

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new TextBlock() { TextAlignment = TextAlignment.Center };
                valuesPanel.Children.Add(values[i]);
            }

            Interpreter = new Interpreter(filePath, addChar, input);

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (autoplay)
            {
                Task.Run(() =>
                {
                    while (Interpreter.CurrentActionsPtr < Interpreter.CurrentActionsLength)
                    {
                        Dispatcher.Invoke(() => Interpreter.Next());
                        Thread.Sleep(debug ? 10 : 0);
                    }
                });
            }
        }
    }
}
