using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace RemoteWindowsController
{
    public partial class ProfileSelector : Window, IRemoteControllable
    {
        private int cursorIndex;
        private ConfigStorage config;
        private List<Profile> profiles;

        public ProfileSelector()
        {
            InitializeComponent();
            Left = SystemParameters.PrimaryScreenWidth / 2 - Width / 2;
            cpuDataFooter.FontFamily = new FontFamily("Consolas");
            config = ((App)Application.Current).config;
            profiles = config.getProfiles();
            ((App)Application.Current).cpuTemp.DataUpdated += CpuTemp_DataUpdated;
            ((App)Application.Current).cpuTemp.readCpuData();
            cursorIndex = config.selectedProfileIndex;
            foreach (Profile profile in profiles)
            {
                Border border = new Border()
                {
                    Margin = new Thickness(5, 0, 0, 0),
                    BorderThickness = new Thickness(6, 0, 0, 0),
                    BorderBrush = Brushes.WhiteSmoke
                };
                Label label = new Label()
                {
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 22,
                    Foreground = Brushes.White,
                    Content = profile.name
                };
                border.Child = label;
                listContainer.Children.Add(border);
            }
            refreshCursor();
        }

        private string parseCpuValue(int val)
        {
            if (val < 10) return "0" + val.ToString();
            return val.ToString();
        }

        private void CpuTemp_DataUpdated(TemperatureMonitor monitor)
        {
            Dispatcher.Invoke(() =>
            {
                cpuDataFooter.Content =
                parseCpuValue(monitor.currentCpuUsage)
                + "% | Temp: "
                + parseCpuValue(monitor.currentCpuTemperature)
                + "°C | Min: "
                + parseCpuValue(monitor.minCpuTemperature)
                + "°C | Max: "
                + parseCpuValue(monitor.maxCpuTemperature)
                + "°C";
            });
        }

        private void refreshCursor()
        {
            for (int i = 0; i < listContainer.Children.Count; i++)
            {
                Border border = (Border)listContainer.Children[i];
                border.BorderThickness = new Thickness(cursorIndex == i ? 6 : 0, 0, 0, 0);
            }
            TextBlock[] textBlocks = new TextBlock[] { slot1, slot2, slot3, slot4, slot5, slot6 };
            Profile selectedProfile = profiles[cursorIndex];
            for(int i = 0; i < textBlocks.Length; i++)
            {
                ProfileItem item = selectedProfile.items[i];
                string value = item.name.Trim() == "" ? item.value : item.name;
                textBlocks[i].Text = value.Trim();
            }
        }

        public void onRemoteButtonPressed(WindowCommand cmd)
        {
            switch (cmd)
            {
                case WindowCommand.Up:
                    cursorIndex -= 1;
                    if (cursorIndex < 0) cursorIndex = profiles.Count - 1;
                    Dispatcher.Invoke(() => refreshCursor());
                    break;
                case WindowCommand.Down:
                    cursorIndex += 1;
                    if (cursorIndex >= profiles.Count) cursorIndex = 0;
                    Dispatcher.Invoke(() => refreshCursor());
                    break;
                case WindowCommand.Select:
                    config.selectedProfileIndex = cursorIndex;
                    ((App)Application.Current).switchActiveWindow(WindowType.None);
                    break;
            }
        }
    }
}
