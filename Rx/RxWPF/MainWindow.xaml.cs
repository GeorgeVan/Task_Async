using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
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

namespace RxWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WhenTextChanged
                .Sample(TimeSpan.FromSeconds(3))
                .Subscribe(x => Debug.WriteLine(DateTime.Now + " Text Changed"));
        }

        public IObservable<TextChangedEventArgs> WhenTextChanged
        {
            get
            {
                return Observable
                    .FromEventPattern<TextChangedEventHandler, TextChangedEventArgs>(
                        h => this.textBox.TextChanged += h, //和控件挂接函数
                        h => this.textBox.TextChanged -= h) //和控件脱离函数
                    .Select(x => x.EventArgs);
            }

            //原理：在上面调用Subscribe的时候，库内部会自动生成一个 TextChangedEventHandler的函数h，然后调用
            //和控件挂接函数；如果在不需要的时候，就把这个函数从控件脱离
        }
    }
}

