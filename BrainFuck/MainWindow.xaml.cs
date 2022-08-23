using System.Windows;

namespace BrainFuck
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string filePath;
#pragma warning disable CS0162 // Code inaccessible détecté
            switch (6)
            {
                case 0:
                    filePath = "HelloWorld.txt";
                    break;
                case 1:
                    filePath = "AroundTheWorld.txt";
                    break;
                case 2:
                    filePath = "overflow.txt";
                    break;
                case 3:
                    filePath = "overflow2.txt";
                    break;
                case 4:
                    filePath = "cat.txt";
                    break;
                case 5:
                    filePath = "cat2.txt";
                    break;
                case 6:
                    filePath = "IDE.bf";
                    break;
            }
#pragma warning restore CS0162 // Code inaccessible détecté
            Interpreter interpreter = new Interpreter(true, $@"..\..\..\tests\{filePath}");
            interpreter.Ended += (sender, args) => MessageBox.Show("ended");
            grid.Children.Add(interpreter);
        }
    }
}
