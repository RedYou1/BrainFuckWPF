using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IDE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public BFProj bFProj;

        private bool changingText = false;

        private string path;
        public string Path
        {
            get => path;
            set
            {
                path = value;
                Properties.Settings.Default.Path = value;
                Properties.Settings.Default.Save();
            }
        }

        public MainWindow()
        {
            path = "";
            InitializeComponent();
            if (Properties.Settings.Default.Path == "" || !Directory.Exists(Properties.Settings.Default.Path))
            {
                OpenFile();
            }
            else
            {
                Path = Properties.Settings.Default.Path;
            }
            bFProj = BFProj.Parse(Path + "/BFProj.json")!;
            UpdateFiles();
        }

        public void OpenFile()
        {
            while (true)
            {
                try
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        CheckFileExists = true,
                        CheckPathExists = true,
                        ValidateNames = false
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        Path = Directory.GetParent(openFileDialog.FileName)!.FullName;
                        bFProj = BFProj.Parse(Path + "/BFProj.json")!;

                        UpdateFiles();
                        UpdateActiveFiles();

                        changingText = true;
                        if (bFProj.CurrentFile is null)
                            txtEditor.Text = "";
                        else
                            txtEditor.Text = bFProj.Files[bFProj.CurrentFile].text;
                        changingText = false;
                        return;
                    }
                }
                catch
                {

                }
            }
        }

        public void ChangeFile(string path)
        {
            if (!bFProj.Files.ContainsKey(path))
            {
                bFProj.Files.Add(path, new BFProj.TextFile { modified = false, text = File.ReadAllText(path) });
            }
            bFProj.CurrentFile = path;
            changingText = true;
            txtEditor.Text = bFProj.Files[bFProj.CurrentFile].text;
            changingText = false;
            UpdateActiveFiles();
        }

        public void AddActiveFile(string path, bool modified)
        {
            FileTab fileTab = new FileTab(System.IO.Path.GetFileName(path) + (modified ? '*' : ""));
            fileTab.name.Background = bFProj.CurrentFile == path ? Brushes.Green : SystemColors.ControlBrush;
            fileTab.name.Click += (sender, args) =>
            {
                ChangeFile(path);
            };
            fileTab.close.Click += (sender, args) =>
            {
                if (bFProj.Files[path].modified && MessageBox.Show("This file is unsaved.\nClose it?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.No)
                {
                    return;
                }
                bFProj.Files.Remove(path);
                if (bFProj.CurrentFile == path)
                {
                    bFProj.CurrentFile = bFProj.Files.Any() ? bFProj.Files.First().Key : null;
                    changingText = true;
                    txtEditor.Text = bFProj.CurrentFile != null ? bFProj.Files[bFProj.CurrentFile].text : "";
                    changingText = false;
                }
                UpdateActiveFiles();
            };
            Grid.SetColumn(fileTab, filesTabes.ColumnDefinitions.Count);
            filesTabes.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            filesTabes.Children.Add(fileTab);
        }

        public void UpdateActiveFiles()
        {
            filesTabes.Children.Clear();
            filesTabes.ColumnDefinitions.Clear();
            foreach (var file in bFProj.Files)
            {
                AddActiveFile(file.Key, file.Value.modified);
            }
        }

        public Compiler.CompileError? Compile()
        {
            bFProj.SaveAllFile();
            UpdateActiveFiles();
            Compiler.Compiler.Debug = chckDebug.IsChecked ?? true;
            return Compiler.Compiler.Compile(Path + "/src/", Path + "/src/" + bFProj.StartingFile, Path + "/build.bf");
        }

        public Compiler.CompileError? Play()
        {
            var r = Compile();
            if (r is not null)
                return r;
            playScreen.Content = new BrainFuckPlayer(Compiler.Compiler.Debug, Path + "/build.bf");
            playScreen.IsSelected = true;
            btnStop.IsEnabled = true;
            return r;
        }

        public void Stop()
        {
            if (playScreen.Content is not null)
            {
                textTab.IsSelected = true;
                playScreen.Content = null;
                btnStop.IsEnabled = false;
            }
        }

        private void AppendFile(UIElement element)
        {
            files.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(element, files.Children.Count);
            files.Children.Add(element);
        }

        private void UpdateFiles()
        {
            files.RowDefinitions.Clear();
            files.Children.Clear();
            if (!Directory.Exists(Path) && !File.Exists(Path))
            {
                AppendFile(new System.Windows.Controls.Label { Content = "This path doesn't exists." });
                return;
            }
            foreach (string path in Directory.EnumerateFiles(Path + "/src/").OrderBy(p => p.EndsWith(".b") ? -2 : p.EndsWith(".f") ? -1 : 0))
            {
                var file = new System.Windows.Controls.Label { Content = System.IO.Path.GetFileName(path) };
                void mouseDown(object sender, RoutedEventArgs args)
                {
                    ChangeFile(path);
                }
                file.MouseDown += new System.Windows.Input.MouseButtonEventHandler(mouseDown);
                AppendFile(file);
            }
        }

        private void btnOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        private void btnNewFile_Click(object sender, RoutedEventArgs e)
        {
            NewFile nf = new NewFile();
            if (nf.ShowDialog() == true &&
                (nf.fileName.Text.EndsWith(".b") || nf.fileName.Text.EndsWith(".f")) &&
                !File.Exists(Path + "/src/" + nf.fileName.Text))
            {
                File.CreateText(Path + "/src/" + nf.fileName.Text).Dispose();
                UpdateFiles();
                ChangeFile(Path + "/src/" + nf.fileName.Text);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bFProj.SaveFile();
            UpdateActiveFiles();
        }

        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            bFProj.SaveAllFile();
            UpdateActiveFiles();
        }

        private void btnCompile_Click(object sender, RoutedEventArgs e)
        {
            var r = Compile();
            if (r is not null)
                MessageBox.Show(r.MessagesToString());
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            var r = Play();
            if (r is not null)
                MessageBox.Show(r.MessagesToString());
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void fileViewer_FileSelected(bool changeDir, string path)
        {
            if (changeDir)
            {
                Path = path;
                return;
            }
            if (File.Exists(path))
                ChangeFile(path);
            else
                Path = path;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bFProj.Files.Any((a) => a.Value.modified))
            {
                if (MessageBox.Show("You have unsaved files.\nClose Application?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                    == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            bFProj.Save(Path + "/BFProj.json");
        }

        private void txtEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (changingText)
                return;
            bFProj.Files[bFProj.CurrentFile!] = new BFProj.TextFile { modified = true, text = txtEditor.Text };
            UpdateActiveFiles();
        }
    }
}
