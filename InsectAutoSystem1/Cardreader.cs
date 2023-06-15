using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsectAutoSystem1
{
    class Cardreader
    {
        private SerialPort serialPort = new SerialPort();
        private string serialPortName;
        private ShowMessageDelegate showMessageDelegate;
        private List<int> receiveData = new List<int>();
        private string cardNumber;

        public Cardreader(string strSerialPortName, ShowMessageDelegate del)
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
            serialPort.Open();

            if (serialPort.IsOpen)
            {
                showMessageDelegate("카드리더가 연결되었습니다.\r\n");
            }
            else
            {
                showMessageDelegate("카드리더 연결에 실패하였습니다.\r\n");
            }
        }

        int count = 0;

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)  //수신 이벤트가 발생하면 이 부분이 실행된다.
        {
            int i_recv_size = serialPort.BytesToRead;
            byte[] b_tmp_buf = new byte[i_recv_size];
            serialPort.Read(b_tmp_buf, 0, i_recv_size);

            foreach (var temp in b_tmp_buf)
            {
                if (temp == 1)
                {
                    receiveData.Clear();
                    count = 0;
                    showMessageDelegate("사육상자 번호를 인식하지 못하였습니다.\r\n");
                    break;
                }

                receiveData.Add(temp);
                count++;
                
                if(count == 11)
                {
                    for(int i=3; i<11; i++)
                    {
                        cardNumber = cardNumber + (char)receiveData[i];
                    }
                    showMessageDelegate("사육상자 번호를 인식하였습니다.\r\n");
                    count = 0;
                    cardNumber = "";
                    receiveData.Clear();
                }
            }
        }

        public string getCardNumber()
        {
            return cardNumber;
        }

        public void read()
        {
            byte[] readByte = { 0x23, 0x03, 0x02, 0x00, 0x02 };
            serialPort.Write(readByte, 0, readByte.Length);
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
