using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
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

namespace Ch28_1_IOOpsGWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PipeDemo pipeDemo;
        private void CreatePipeDemoIf()
        {
            if (pipeDemo == null)
            {
                pipeDemo = new PipeDemo(Convert.ToInt32(textBoxTalkCount.Text), Convert.ToInt32(textBoxSlotCount.Text));
                textBoxTalkCount.IsEnabled = false;
                textBoxSlotCount.IsEnabled = false;
            }
        }

        private string getClientProgress(string defaultValue)
        {
            double[] progress = pipeDemo.GetActiveSlotsProgress();
             
            return progress.Length == 0 ? defaultValue : string.Format("总共{0}, [{1:0.##}, {2:0.##}], 平均 {3:0.##}", progress.Length, progress.Min(),progress.Max(),progress.Average() );
        }

        public MainWindow()
        {
            InitializeComponent();
            Observable
                .Interval(TimeSpan.FromMilliseconds(100), DispatcherScheduler.Current)
                .Subscribe( _ =>
                {
                    if (pipeDemo == null) return;
                     
                    PipeDemo.PipeInfo pi = pipeDemo.CurrentPipeInfo;
                    /* labelMsg.Content = "Created: " + pi.created +
                    ", connected: " + pi.connected + 
                    ", sstage1: " + pi.sstage1 + ", sstage2: " + pi.sstage2 + ", sstage3: " + pi.sstage3 + ", sexp: " + pi.sexp + ", sclosed: " + pi.sclosed +
                    ", cstage1: " + pi.cstage1 + ", cstage2: " + pi.cstage2 + ", cstage3: " + pi.cstage3 + ", cexp: " + pi.cexp + ", cclosed: " + pi.cclosed + 
                    ", completed: "+pi.completed;
                    */
                     label.Content = "Created: " + pi.created;
                    label1.Content = "scompleted: " + pi.scompleted;
                    label2.Content = "ccompleted: " + pi.ccompleted;
                    label17.Content = "Server读慢 " + ((pi.sstage2 - pi.sstage3)- (pi.cstage2 - pi.cstage3));
                    //                                   ServerReading                ClientWriting

                    label18.Content = "Server写慢" + ((pi.sstage3 - pi.sclosed) - (pi.cstage3 - pi.cclosed));
                    //                                   ServerWriting                ClientReading

                    label3.Content = "sstage1: " + pi.sstage1;
                    label4.Content = "sstage2: " + pi.sstage2;
                    label5.Content = "sstage3: " + pi.sstage3;
                    label6.Content = "sexp: " + pi.sexp;
                    label7.Content = "s取消: " + pi.scanceled;


                    label8.Content = "cstage1: " + pi.cstage1;
                    label9.Content = "cstage2: " + pi.cstage2;
                    label10.Content = "cstage3: " + pi.cstage3;
                    label11.Content = "cexp: " + pi.cexp;
                    label12.Content = "c取消: " + pi.ccanceled ;

                    label13.Content = "sfirstOP: " + (pi.sstage2 - pi.sstage3);
                    label14.Content = "cfirstOP: " + (pi.cstage2 - pi.cstage3);

                    label15.Content = "slastOP: " + (pi.sstage3 - pi.sclosed);
                    label16.Content = "clastOP: " + (pi.cstage3 - pi.cclosed);

                    label19.Content = "活动服务: " + (pi.sstage1 - pi.sclosed);
                    label20.Content = "活动客户: " + (pi.cstage1 - pi.cclosed);

                    if (pi.order > 0)
                    {
                        progressBarClient.Value = ((double)pi.cstage3 * progressBarClient.Maximum ) / (pipeDemo.TalkCount * pi.order);
                        progressBarServer.Value = ((double)pi.sstage3 * progressBarClient.Maximum) / (pipeDemo.TalkCount * pi.order);
                    }

                    labelMsg.Content = getClientProgress(labelMsg.Content.ToString() );
                });
        }

        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();
            buttonStartServer1.IsEnabled = false;

            if (checkBoxPureAsync.IsChecked ?? false)
            {
                //因为content的原因，无论何时都要在新线程中启动
                await Task.Run(() => pipeDemo.StartServerAsync(true));
            }
            else
            {
                await Task.Run(() => pipeDemo.StartServerAsync(false));
            }
            buttonStartServer1.IsEnabled = true;
        }

        private async void buttonStartClient_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();

            labelMsg.Content = "Running";
            buttonStartClient.IsEnabled = false;
            buttonStartClient2.IsEnabled = false;
            int clientCount = Convert.ToInt32(textBoxClientCount.Text);
            TimeSpan ts = await Task.Run(()=> pipeDemo.StartClientAsync(clientCount));
            //TimeSpan ts = await PipeDemo.StartClientAsync(clientCount);
            buttonStartClient.IsEnabled = true;
            buttonStartClient2.IsEnabled = true;
            labelMsg.Content = "总共花费：" + ts;
        }

        private void buttonResetData_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();
            pipeDemo.ResetPipeInfo();
        }

        private async  void buttonStartClient2_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();
            labelMsg.Content = "Running";
            buttonStartClient.IsEnabled = false;
            buttonStartClient2.IsEnabled = false;
            TimeSpan ts = await pipeDemo.StartClientAsync2(Convert.ToInt32(textBoxClientCount.Text));
            buttonStartClient.IsEnabled = true;
            buttonStartClient2.IsEnabled = true;
            labelMsg.Content = "总共花费：" + ts;
        }

        private void buttonCancelServer_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();
            if (!pipeDemo.CancelServer())
            {
                MessageBox.Show ("Cannot cancel server because 同步");
            }
        }

        private void buttonCancelClient_Click(object sender, RoutedEventArgs e)
        {
            CreatePipeDemoIf();
            pipeDemo.CancelClient();
        }

        private void buttonShowException_Click(object sender, RoutedEventArgs e)
        {
            WindowMessage win = new WindowMessage(pipeDemo.ExceptionLogs);
            win.Show();
        }
    }
}
