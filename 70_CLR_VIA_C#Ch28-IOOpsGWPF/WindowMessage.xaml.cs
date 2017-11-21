using System;
using System.Collections.Concurrent;
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

namespace Ch28_1_IOOpsGWPF
{
    /// <summary>
    /// Interaction logic for WindowMessage.xaml
    /// </summary>
    public partial class WindowMessage : Window
    {
        IEnumerable<string> _Messages;
        public WindowMessage(IEnumerable<string> Messages)
        {
            _Messages = Messages;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxMsg.Items.Clear();
            foreach (var v in _Messages)
                listBoxMsg.Items.Add(v);
        }
    }
}
