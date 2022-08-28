using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace IDE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string FilePath
        {
            get => (string)lblFilePath.Content;
            set
            {
                lblFilePath.Content = value;
                Properties.Settings.Default.FilePath = value;
                Properties.Settings.Default.Save();
                txtEditor.Text = File.ReadAllText(value);
                if (File.Exists(Path.ChangeExtension(value, "bf")))
                {
                    txtEditorCompiled.Text = File.ReadAllText(Path.ChangeExtension(value, "bf"));
                }
            }
        }

        public TabItem? PlayTab { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            if (Properties.Settings.Default.FilePath == "" || !File.Exists(Properties.Settings.Default.FilePath))
            {
                OpenFile();
            }
            else
            {
                FilePath = Properties.Settings.Default.FilePath;
            }
        }

        public void OpenFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
            }
            else if (FilePath == "")
            {
                Close();
            }
        }

        public void Save()
        {
            StreamWriter sw = File.CreateText(FilePath);
            sw.Write(txtEditor.Text);
            sw.Flush();
            sw.Close();
        }

        public void Compile()
        {
            Save();
            Compiler.Compiler.Compile(FilePath, Path.ChangeExtension(FilePath, "bf"));
        }

        public void Play()
        {
            if (PlayTab is not null)
            {
                Stop();
            }
            Compile();
            PlayTab = new TabItem()
            {
                Header = "Play",
                Content = new BrainFuck.Interpreter(true, Path.ChangeExtension(FilePath, "bf"))
            };
            screen.Items.Add(PlayTab);
            screen.SelectedItem = PlayTab;
            btnStop.IsEnabled = true;
        }

        public void Stop()
        {
            if (PlayTab is not null)
            {
                screen.Items.Remove(PlayTab);
                PlayTab = null;
                btnStop.IsEnabled = false;
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void btnCompile_Click(object sender, RoutedEventArgs e)
        {
            Compile();
            txtEditorCompiled.Text = File.ReadAllText(Path.ChangeExtension(FilePath, "bf"));
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }
    }
}
