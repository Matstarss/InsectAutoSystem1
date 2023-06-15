using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace InsectAutoSystem1
{
    delegate void ShowVideoFrameDelegate(Bitmap videoFrame);
    delegate void ShowMessageDelegate(String Str);
    delegate void MonitorControllerDataDelegate(String strData);
    

    public partial class Form1 : Form
    {
        private Camera camera;
        private Scale scale;
        private Controller controller;
        private Cardreader cardreader;
        private Diary diary;
        private UpdateImage uploadImaage;

        private Thread getWeightThread;
        private Thread getDeviceInfoThread;

        private bool scaleConnectCheck;
        private bool cardreaderConnectCheck;
        private bool controllerConnectCheck;
        private float weight;
        private bool motorRun = false;


        public Form1()
        {
            InitializeComponent();
            getWeightThread = new Thread(refreshWeight);
            getDeviceInfoThread = new Thread(getDeviceInfo);
            scaleConnectCheck = false;
            cardreaderConnectCheck = false;
            controllerConnectCheck = false;
            diary = new Diary();
            uploadImaage = new UpdateImage();
        }

        private void init()
        {
            //초기화
            DeviceState.setFeedState(DeviceState.FeedState.None);
            weight = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ShowVideoFrameDelegate del = showVideoFrame;
            ShowMessageDelegate del1 = showMessage;
            camera = new Camera(del, del1);
            setSerialPort();
            init();
        }

        private void getDeviceInfo()
        {
            while (true)
            {
                controller.sendCommand("get_info");
                Thread.Sleep(2000);
            }
        }

        private void monitorControllerData(String strData)
        {
            var responseValues = strData.Split(',');
            if (Int32.Parse(responseValues[3])==1) //센서1에 물체가 감지되면
            {
                if (DeviceState.getFeedState() == DeviceState.FeedState.None) //TODO end도 넣어야 하지 않나?
                {
                    if (weight > DeviceState.targetFeedWeight)
                    {
                        DeviceState.setFeedState(DeviceState.FeedState.End);
                    }
                    else
                    {
                        DeviceState.setFeedState(DeviceState.FeedState.NewBox);
                        feed();
                    }
                }
            }
            else if(Int32.Parse(responseValues[3]) == 0)
            {
                DeviceState.setFeedState(DeviceState.FeedState.None);
            }

            if (DeviceState.getFeedState() == DeviceState.FeedState.End)
            {
                if(Int32.Parse(responseValues[1]) == 0)
                {
                    controller.sendCommand("motor_run");
                }
            }
        }

        private void setSerialPort()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var portnames = SerialPort.GetPortNames();
                var ports = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());

                foreach(string port in ports)
                {
                    if (port.Contains("COM102"))
                    //if (port.Contains("USB Serial Port"))
                    {
                        ShowMessageDelegate del1 = showMessage;
                        MonitorControllerDataDelegate del2 = monitorControllerData;
                        string str = port.Split('(')[1];
                        str = str.Replace(" ", "");
                        str = str.Replace(")", "");
                        controller = new Controller(str, del2, del1);
                    }

                    if (port.Contains("COM101"))
                    //if (port.Contains("Prolific USB"))
                    {
                        ShowMessageDelegate del = showMessage;
                        string str = port.Split('(')[1];
                        str = str.Replace(" ", "");
                        str = str.Replace(")", "");
                        scale = new Scale(str, del);
                        scale.setSerialPort();
                        getWeightThread.Start();

                    }

                    if (port.Contains("COM103"))
                    //if (port.Contains("Silicon Labs CP210x USB to UART Bridge"))
                    {
                        ShowMessageDelegate del = showMessage;
                        string str = port.Split('(')[1];
                        str = str.Replace(" ", "");
                        str = str.Replace(")", "");
                        cardreader = new Cardreader(str, del);
                        cardreader.setSerialPort();
                    }
                }

                cbScalePort.DataSource = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
                cbControlPort.DataSource = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
                cbCardreaderPort.DataSource = portnames.Select(n => n + " - " + ports.FirstOrDefault(s => s.Contains(n))).ToList();
            }
        }

        private void showVideoFrame(Bitmap videoFrame)
        {
            if (pictureBox1.Image != null)
            {
                pictureBox1.Image.Dispose();
            }
            try
            { 
                pictureBox1.Image = videoFrame;
            }
            catch (Exception ex)
            {
                tbLog.Text += ex.Message + "\r\n";
            }
        }


        private void showMessage(string str)
        {
            this.Invoke(new Action(delegate () { 
                tbLog.Text += str + "\n";
                if (str == "저울이 연결되었습니다.\r\n")
                {
                    cbScalePort.Enabled = false;
                    btnConnectScale.Enabled = false;
                    scaleConnectCheck = true;
                }
                if (str == "제어기가 연결되었습니다.\r\n")
                {
                    cbControlPort.Enabled = false;
                    btnConnectController.Enabled = false;
                    controllerConnectCheck = true;
                }
                if (str == "카드리더가 연결되었습니다.\r\n")
                {
                    cbCardreaderPort.Enabled = false;
                    btnConnectCardreader.Enabled = false;
                    cardreaderConnectCheck = true;
                }
                if (str == "사육상자 번호를 인식하였습니다.\r\n")
                {
                    tbBoxCode.Text = cardreader.getCardNumber();
                    Thread.Sleep(1000);
                    camera.makeSnapshot(cardreader.getCardNumber());
                    Console.WriteLine("현재시간 : " + DateTime.Now.ToString());
                    Thread.Sleep(1000);
                    controller.sendCommand("motor_run");
                }
            }));
        }

        private void btnConnectScale_Click(object sender, EventArgs e)
        {
            ShowMessageDelegate del = showMessage;
            string str = (cbScalePort.Text).Split('-')[0];
            str = str.Replace(" ", "");
            scale = new Scale(str, del);
            scale.setSerialPort();
            getWeightThread.Start();
        }

        private void refreshWeight()
        {
            while (true)
            {
                weight = scale.getWeight();
                if (DeviceState.getFeedState() == DeviceState.FeedState.Feeding) //셔틀동작하고 있을때
                { 
                    if (weight >= DeviceState.targetFeedWeight)
                    {
                        DeviceState.setFeedState(DeviceState.FeedState.Full);
                        controller.sendCommand("shuttle_stop");
                        controller.sendCommand("motor_run");
                        DeviceState.setFeedState(DeviceState.FeedState.End);
                    }
                }
                this.Invoke(new Action(delegate () {
                    tbWeight.Text = weight.ToString();
                }));
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'a' || e.KeyChar == 'A')
            {
                if (motorRun)
                {
                    cardreader.read();
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            motorRun = false;
            controller.sendCommand("motor_stop");
            controller.sendCommand("shuttle_stop");
            tbFeedWeight.Enabled = true;
            tbStartWeight.Enabled = true;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }

        private void btnConnectController_Click(object sender, EventArgs e)
        {
            ShowMessageDelegate del1 = showMessage;
            MonitorControllerDataDelegate del2 = monitorControllerData;
            string str = (cbControlPort.Text).Split('-')[0];
            str = str.Replace(" ", "");
            controller = new Controller(str, del2, del1);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!(scaleConnectCheck || controllerConnectCheck || cardreaderConnectCheck))
            {
                showMessage("제어 연결이 완료되지 않아 시작할 수 없습니다.\r\n");
                return;
            }
            motorRun = true;
            DeviceState.targetFeedWeight = Double.Parse(tbStartWeight.Text) + Double.Parse(tbFeedWeight.Text);
            tbFeedWeight.Enabled = false;
            tbStartWeight.Enabled = false;

            if (!getDeviceInfoThread.IsAlive) 
            {
                getDeviceInfoThread.Start();
            }
            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void feed()
        {
            if (DeviceState.getFeedState() == DeviceState.FeedState.NewBox)
            {
                DeviceState.setFeedState(DeviceState.FeedState.Feeding);
                controller.sendCommand("shuttle_run");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            camera.clear();
            pictureBox1.Image = null;
            pictureBox1.Invalidate();

            if (getWeightThread.IsAlive)
            {
                getWeightThread.Abort();
            }

            if (getDeviceInfoThread.IsAlive)
            {
                getDeviceInfoThread.Abort();
            }

            if (cardreader != null)
            {
                cardreader.close();
            }

            if (controller != null)
            {
                controller.close();
            }
            
            if (scale != null)
            {
                scale.close();
            }

            Application.Exit();
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!(char.IsDigit(e.KeyChar) || e.KeyChar == Convert.ToChar(Keys.Back)))    //숫자와 백스페이스를 제외한 나머지를 바로 처리             
            {
                e.Handled = true;
            }
        }

        private void Form1_FormClosing(object sender, EventArgs e)
        {
            MessageBox.Show("closing");
        }


        private async void label3_Click(object sender, EventArgs e)
        {
            //await uploadImaage.UploadImages("http://localhost:3005/ifactory/api/file/uploadfile/{uploadPath}/{id}", "C:/test/test.jpeg", "diary", diaryId);
           
            int diaryId = await diary.post();
            //이미지 업로드 메소드
            await uploadImaage.UploadImages("http://59.15.133.179:23500/ifactory/api/file/uploadfile/{uploadPath}/{id}", "C:/test/test.jpeg", "diary", diaryId);
        }

        private async void label4_Click(object sender, EventArgs e)
        {
        }
    }
}
