using System;
using System.Collections.Generic;
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

namespace IDE
{
    /// <summary>
    /// Logique d'interaction pour FileTab.xaml
    /// </summary>
    public partial class FileTab : Grid
    {
        public FileTab(string name)
        {
            InitializeComponent();
            this.name.Content = name;
        }
    }
}
