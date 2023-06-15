using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsectAutoSystem1
{
    class Controller
    {
        SerialPort serialPort = new SerialPort();
        private MonitorControllerDataDelegate monitorControllerDataDelegate;
        private ShowMessageDelegate showMessageDelegate;

        public Controller(string serialPortName, MonitorControllerDataDelegate del, ShowMessageDelegate del1)
        {
            serialPort.PortName = serialPortName;  // TODO: 장치이름으로 자동으로 잡히게 해야 한다.
            serialPort.BaudRate = 19200;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;
            serialPort.Handshake = Handshake.None; // 핸드셰이크
            serialPort.Encoding = System.Text.Encoding.ASCII; // 인코딩 방식
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            monitorControllerDataDelegate = del;
            showMessageDelegate = del1;
            serialPort.Open();

            if (serialPort.IsOpen)
            {
                showMessageDelegate("제어기가 연결되었습니다.\r\n");
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var readData = serialPort.ReadLine();
            monitorControllerDataDelegate(readData);
        }

        public void sendCommand(String command)
        {
            if (command == "motor_run")
            {
                if(DeviceState.getFeedState() == DeviceState.FeedState.None || DeviceState.getFeedState() == DeviceState.FeedState.End || DeviceState.getFeedState() == DeviceState.FeedState.Full) { 
                    serialPort.Write("!");
                    byte[] bytes = { 0x01, 0x01, 0x9A, 0xE1 };
                    serialPort.Write(bytes, 0, 4);
                    serialPort.Write("\r\n");
                }
            }

            if (command == "motor_stop")
            {
                serialPort.Write("!");
                byte[] bytes = { 0x01, 0x00, 0x5a, 0x20 };
                serialPort.Write(bytes, 0, 4);
                serialPort.Write("\r\n");
            }

            if (command == "shuttle_run")
            {
                serialPort.Write("!");
                byte[] bytes = { 0x03, 0x01, 0xfa, 0xe0 };
                serialPort.Write(bytes, 0, 4);
                serialPort.Write("\r\n");
            }

            if (command == "shuttle_stop")
            {
                serialPort.Write("!");
                byte[] bytes = { 0x03, 0x00, 0x3a, 0x21 };
                serialPort.Write(bytes, 0, 4);
                serialPort.Write("\r\n");
            }

            if (command == "get_info")
            {
                serialPort.Write("!");
                byte[] bytes = { 0x00, 0x01, 0x0a, 0xe0};
                serialPort.Write(bytes, 0, 4);
                serialPort.Write("\r\n");
            }
        }

        public void close()
        {
            if(serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        
    }
}

