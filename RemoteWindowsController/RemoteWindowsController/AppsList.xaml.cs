using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoteWindowsController
{
    public partial class AppsList : Window, IRemoteControllable
    {
        private int cursorIndex = 0;
        private ConfigStorage config;
        private List<AppShortcut> apps;

        public AppsList()
        {
            InitializeComponent();
            config = ((App)Application.Current).config;
            apps = config.getAppShortcuts();
            foreach(AppShortcut app in apps)
            {
                Border border = new Border() {
                    Margin = new Thickness(5, 0, 0, 0),
                    BorderThickness = new Thickness(6, 0, 0, 0),
                    BorderBrush = Brushes.Black
                };
                Label label = new Label() {
                    Margin = new Thickness(5, 0, 0, 0),
                    FontSize = 22,
                    Content = app.name
                };
                border.Child = label;
                listContainer.Children.Add(border);
            }
            refreshCursor();
        }

        private void refreshCursor()
        {
            for (int i = 0; i < listContainer.Children.Count; i++)
            {
                Border border = (Border)listContainer.Children[i];
                border.BorderThickness = new Thickness(cursorIndex == i ? 6 : 0, 0, 0, 0);
            }

        }

        public void onRemoteButtonPressed(WindowCommand cmd)
        {
            switch (cmd)
            {
                case WindowCommand.Up:
                    cursorIndex -= 1;
                    if (cursorIndex < 0) cursorIndex = apps.Count - 1;
                    Dispatcher.Invoke(() => refreshCursor());
                    break;
                case WindowCommand.Down:
                    cursorIndex += 1;
                    if (cursorIndex >= apps.Count) cursorIndex = 0;
                    Dispatcher.Invoke(() => refreshCursor());
                    break;
                case WindowCommand.Select:
                    AppShortcut selectedApp = apps[cursorIndex];
                    try
                    {
                        Process.Start(selectedApp.path, selectedApp.args);
                        ((App)Application.Current).switchActiveWindow(WindowType.None);
                    } catch { }
                    break;
            }
        }
    }
}
