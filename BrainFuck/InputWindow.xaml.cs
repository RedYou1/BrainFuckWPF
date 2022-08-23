using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace BrainFuck
{
    /// <summary>
    /// Logique d'interaction pour InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow(int maxLength)
        {
            InitializeComponent();
            Message.MaxLength = maxLength;
            MaxLength = maxLength;
        }

        public bool Finished { get; protected set; } = false;
        public bool IsString => isString?.IsChecked ?? false;
        public string Text => (string)CurrentMessage.Content;
        public int MaxLength { get; protected set; }

        private void Message_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsString)
            {
                CurrentMessage.Content = Message.Text;
            }
        }

        private void firstOk_Click(object sender, RoutedEventArgs e)
        {
            nextPage(true);
            Message.Text = "";
            CurrentMessage.Content = "";
            Message.IsEnabled = true;
        }

        private void prev_Click(object sender, RoutedEventArgs e)
        {
            if (IsString || Text.Length == 0)
            {
                nextPage(false);
            }
            else
            {
                Message.Text = $"{Encoding.ASCII.GetBytes($"{Text[Text.Length - 1]}")[0]}";
                CurrentMessage.Content = Text.Substring(0, Text.Length - 1);
                Message.IsEnabled = true;
            }
        }

        private void next_Click(object sender, RoutedEventArgs e)
        {
            byte value;
            bool success = byte.TryParse(Message.Text, out value);
            if (success)
            {
                CurrentMessage.Content = Text + (char)value;
                Message.Text = "";
                if (Text.Length == MaxLength)
                {
                    Message.IsEnabled = false;
                }
            }
            else
            {
                byte[] values = Encoding.ASCII.GetBytes(Message.Text);
                foreach (byte v in values)
                {
                    CurrentMessage.Content = Text + (char)v;
                    if (Text.Length == MaxLength)
                    {
                        Message.IsEnabled = false;
                        break;
                    }
                }
                Message.Text = "";
            }
        }

        private void finish_Click(object sender, RoutedEventArgs e)
        {
            Finished = true;
            Close();
        }

        public void nextPage(bool next)
        {
            firstOk.Visibility = next ? Visibility.Hidden : Visibility.Visible;
            isString.Visibility = next ? Visibility.Hidden : Visibility.Visible;
            messageGird.Visibility = next ? Visibility.Visible : Visibility.Hidden;
            prev.Visibility = next ? Visibility.Visible : Visibility.Hidden;
            this.next.Visibility = next && !IsString ? Visibility.Visible : Visibility.Hidden;
            finish.Visibility = next ? Visibility.Visible : Visibility.Hidden;

            if (next)
            {
                if (IsString)
                {
                    Title.Content = $"Write the text max {MaxLength} characteres";
                }
                else
                {
                    Title.Content = $"Write bytes max {MaxLength} bytes";
                }
            }
            else
            {
                Title.Content = "Choose the methode of input.";
            }
        }
    }
}
