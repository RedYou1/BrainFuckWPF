using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using Compiler;
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
            }
        }
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
            Save();
            Compiler.Compiler.Compile(FilePath, Path.ChangeExtension(FilePath, "bf"));
        }
    }
}
