using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace RxWin
{
    public partial class Form1 : Form
    {
        public Form1()
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
                        h => this.textBox1.TextChanged += h,
                        h => this.textBox1.TextChanged -= h)
                    .Select(x => x.EventArgs);
            }
        }



    }
}
