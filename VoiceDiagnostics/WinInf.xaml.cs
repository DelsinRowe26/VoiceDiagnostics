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
using System.Windows.Shapes;

namespace VoiceDiagnostics
{
    /// <summary>
    /// Логика взаимодействия для WinInf.xaml
    /// </summary>
    public partial class WinInf : Window
    {
        public static int RecInd = 0;
        public WinInf()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WinInf1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            RecInd = 1;
        }
    }
}
