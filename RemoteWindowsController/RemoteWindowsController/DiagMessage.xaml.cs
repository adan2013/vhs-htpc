using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace RemoteWindowsController
{
    /// <summary>
    /// Interaction logic for DiagMessage.xaml
    /// </summary>
    public partial class DiagMessage : Window
    {
        public DiagMessage()
        {
            InitializeComponent();
            App app = ((App)Application.Current);

            bool portConnected = app.serialMonitor.isConnected();
            bool temperatureMonitor = app.cpuTemp.currentCpuUsage > 0;

            status.Text =
                "Connection: "
                + (portConnected ? "OK" : "ERROR")
                + "\nCPU monitor: "
                + (temperatureMonitor ? "OK" : "ERROR");
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(5d);
            timer.Tick += TimerTick;
            timer.Start();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            DispatcherTimer timer = (DispatcherTimer)sender;
            timer.Stop();
            timer.Tick -= TimerTick;
            Close();
        }
    }
}
