﻿using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteWindowsController
{
    public class SerialMonitor
    {
        SerialPort port;

        public readonly string portName;

        public delegate void DataReceivedDelegate(string content);
        public event DataReceivedDelegate DataReceived;

        public SerialMonitor()
        {
            try
            {
                string[] existingPorts = SerialPort.GetPortNames();
                System.Diagnostics.Trace.WriteLine(existingPorts.Length + " com port found");
                if (existingPorts.Length > 0)
                {
                    portName = existingPorts[existingPorts.Length - 1];
                    port = new SerialPort(portName, 115200);
                    port.Open();
                    if (port.IsOpen)
                    {
                        System.Diagnostics.Trace.WriteLine("Listening on port '" + portName + "'");
                        port.DataReceived += Port_DataReceived;
                    }
                }
            }
            catch { }
        }

        public bool isConnected() => port != null && port.IsOpen;

        public bool sendData(string content)
        {
            if (isConnected())
            {
                System.Diagnostics.Trace.WriteLine("Sending: " + content);
                port.Write(content + "\n");
                return true;
            }
            return false;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if(port != null && port.IsOpen)
            {
                string data = port.ReadExisting().Replace("\n", "").Replace("\r", "");
                System.Diagnostics.Trace.WriteLine("Data Received: " + data);
                if (data.Substring(0, 1) == "B")
                {
                    DataReceived?.Invoke(data.Substring(1));
                }
            }
        }
    }
}
