using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WindowsInput.Native;
using WindowsInput;

namespace RemoteWindowsController
{
    public class RemoteControlInterpreter
    {
        InputSimulator inputSim = new InputSimulator();
        App mainApp;

        public RemoteControlInterpreter(App mainApp)
        {
            this.mainApp = mainApp;
        }

        private void typeSlotValue(int slotNumber)
        {
            string slotValue = mainApp.config.getSlotValue(slotNumber);
            if (slotValue.Length > 0) inputSim.Keyboard.TextEntry(slotValue);
        }

        public bool receiveButtonCode(string data)
        {
            switch (data)
            {
                case "1": // profile
                    mainApp.switchActiveWindow(WindowType.Profiles);
                    break;
                case "2": // mute
                    inputSim.Keyboard.KeyPress(VirtualKeyCode.VOLUME_MUTE);
                    break;
                case "3": // full screen
                    inputSim.Keyboard.KeyPress(VirtualKeyCode.F11);
                    break;
                case "4": // reload
                    inputSim.Keyboard.KeyPress(VirtualKeyCode.F5);
                    break;
                case "5": // switch tabs
                    inputSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.TAB);
                    break;
                case "6": // switch windows
                    inputSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.TAB);
                    break;
                case "7": // volume up
                    if (mainApp.windowsAreClosed())
                    {
                        inputSim.Keyboard.KeyPress(VirtualKeyCode.VOLUME_UP);
                    }
                    else
                    {
                        mainApp.sendCommandToOpenWindow(WindowCommand.Up);
                    }
                    break;
                case "8": // volume down
                    if (mainApp.windowsAreClosed())
                    {
                        inputSim.Keyboard.KeyPress(VirtualKeyCode.VOLUME_DOWN);
                    }
                    else
                    {
                        mainApp.sendCommandToOpenWindow(WindowCommand.Down);
                    }
                    break;
                case "9": // enter
                    if (mainApp.windowsAreClosed())
                    {
                        inputSim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    }
                    else
                    {
                        mainApp.sendCommandToOpenWindow(WindowCommand.Select);
                    }
                    break;
                case "10": // paste
                    inputSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                    break;
                case "11": // slot 1
                    typeSlotValue(1);
                    break;
                case "12": // slot 2
                    typeSlotValue(2);
                    break;
                case "13": // slot 3
                    typeSlotValue(3);
                    break;
                case "14": // slot 4
                    typeSlotValue(4);
                    break;
                case "15": // slot 5
                    typeSlotValue(5);
                    break;
                case "16": // slot 6
                    typeSlotValue(6);
                    break;
                case "17": // apps
                    mainApp.switchActiveWindow(WindowType.Apps);
                    break;
                case "18": // clear
                    inputSim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_A);
                    inputSim.Keyboard.KeyPress(VirtualKeyCode.DELETE);
                    break;
                default:
                    System.Diagnostics.Trace.WriteLine("Remote code not recognised: " + data);
                    return false;
            }
            return true;
        }
    }
}
