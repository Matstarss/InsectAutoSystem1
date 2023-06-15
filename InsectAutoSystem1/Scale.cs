using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;  //시리얼통신을 위해 추가해줘야 함
using System.Management;
using System.Collections;

namespace InsectAutoSystem1
{
    class Scale
    {
        private float weight = 0;
        private SerialPort serialPort = new SerialPort();
        private string serialPortName;
        private ShowMessageDelegate showMessageDelegate;

        public Scale(string strSerialPortName, ShowMessageDelegate del)
        {
            showMessageDelegate = del;
            serialPortName = strSerialPortName;
        }
        
        public void setSerialPort()
        {
            serialPort.PortName = serialPortName;  // TODO: 장치이름으로 자동으로 잡히게 해야 한다.
            serialPort.BaudRate = 9600;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Parity = Parity.None;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived);
            if(serialPort.IsOpen)
            {
                serialPort.Close();
            }

            serialPort.Open();



            if (serialPort.IsOpen)
            {
                showMessageDelegate("저울이 연결되었습니다.\r\n");
            }
            else
            {
                showMessageDelegate("저울 연결을 실패하였습니다.\r\n");
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)  //수신 이벤트가 발생하면 이 부분이 실행된다.
        {
            string str = serialPort.ReadLine();
            str = str.Replace(" ", string.Empty);
            try
            {
                weight = float.Parse(str);
            }
            catch(Exception ex)
            {

            }
        }

        public float getWeight()
        {
            return weight;
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
