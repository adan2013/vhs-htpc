using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RemoteWindowsController
{
    
    public enum WindowType
    {
        None,
        Profiles,
        Apps,
        Settings
    }

    public enum WindowCommand
    {
        Up,
        Down,
        Select
    }

    public partial class App : Application
    {
        public ConfigStorage config = new ConfigStorage();
        public SerialMonitor serialMonitor = new SerialMonitor();
        public RemoteControlInterpreter remote;
        
        WindowType activeWindow = WindowType.None;
        Window? openedWindowRef;
            
        public App()
        {
            remote = new RemoteControlInterpreter(this);
            serialMonitor.DataReceived += SerialMonitor_DataReceived;
        }

        private void SerialMonitor_DataReceived(string content)
        {
            remote.receiveButtonCode(content);
        }

        private void showActiveWindow(WindowType type)
        {
            activeWindow = type;
            Thread thread = new Thread(() =>
            {
                switch (activeWindow)
                {
                    case WindowType.Profiles:
                        openedWindowRef = new ProfileSelector();
                        break;
                    case WindowType.Apps:
                        openedWindowRef = new AppsList();
                        break;
                    case WindowType.Settings:
                        openedWindowRef = new StorageManager();
                        break;
                    default:
                        openedWindowRef = null;
                        break;
                }
                if (openedWindowRef != null)
                {
                    openedWindowRef.Show();
                    openedWindowRef.Activate();
                    System.Windows.Threading.Dispatcher.Run();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void closeActiveWindow()
        {
            openedWindowRef?.Dispatcher.Invoke(openedWindowRef.Close);
            activeWindow = WindowType.None;
        }

        public void switchActiveWindow(WindowType type)
        {
            if (type == WindowType.Profiles && activeWindow == WindowType.Apps)
            {
                closeActiveWindow();
                showActiveWindow(WindowType.Settings);
            }
            else
            {
                if (activeWindow != WindowType.None)
                {
                    closeActiveWindow();
                }
                else
                {
                    showActiveWindow(type);
                }
            }
        }

        public bool windowsAreClosed() => activeWindow == WindowType.None;

        public void sendCommandToOpenWindow(WindowCommand cmd)
        {
            if (activeWindow != WindowType.None && openedWindowRef is IRemoteControllable)
            {
                ((IRemoteControllable)openedWindowRef).onRemoteButtonPressed(cmd);
            }
        }
    }
}
