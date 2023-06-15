using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsectAutoSystem1
{
    class Camera
    {
        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;
        private VideoCaptureDeviceForm captureDevice = new VideoCaptureDeviceForm();
        private Bitmap videoFrame;
        private ShowVideoFrameDelegate showVideoFrameDelegate;
        private ShowMessageDelegate showMessageDelegate;

        public Camera(ShowVideoFrameDelegate del, ShowMessageDelegate del1)
        {
            showVideoFrameDelegate = del;
            showMessageDelegate = del1;
            initCamera();
        }

        private void initCamera()
        {
            int cameraIndex = -1;
            int i = 0;
            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            foreach (FilterInfo device in VideoCaptureDevices)
            {
                if (device.Name == "See3CAM_CU135")
                {
                    cameraIndex = i;
                }
                i++;
            }

            if (cameraIndex != -1)
            {
                captureDevice = new VideoCaptureDeviceForm();
                FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[cameraIndex].MonikerString);
                FinalVideo.NewFrame += new NewFrameEventHandler(FinalVideo_NewFrame);

                FinalVideo.VideoResolution = FinalVideo.VideoCapabilities[4];
                FinalVideo.Start();
            }
            else
            {
                showMessageDelegate("카메라가 없습니다.\r\n카메라를 연결을 확인해 주세요.\r\n");
            }
        }

        private void FinalVideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            videoFrame = (Bitmap)eventArgs.Frame.Clone();
            showVideoFrameDelegate(videoFrame);
        }

        public bool makeSnapshot(string cardNumber)
        {
            //string filePath = "d:\\" + cardNumber + ".bmp";
            string filePath = "c:\\Users\\USER\\Documents\\자동급이장치\\" + cardNumber + ".bmp";
            if (File.Exists(filePath)) // 파일이 존재하는 경우 memory leak이 발생한다.
            {
                // 파일을 삭제하여 memory leak을 방지한다.
                File.Delete(filePath);
            }

            // 이미지를 저장할 경로와 형식을 설정합니다.
            ImageFormat format = ImageFormat.Bmp; // 이미지 형식은 PNG로 설정합니다.
            ImageCodecInfo encoder = GetEncoder(format); // ImageCodecInfo 객체를 가져옵니다.
            EncoderParameters encoderParams = GetEncoderParameters(format); // EncoderParameter 객체 배열을 가져옵니다.

            if (File.Exists(filePath)) // 파일이 존재하는 경우
            {
                // 파일 삭제
                File.Delete(filePath);
            }

            // 이미지를 저장합니다.
            videoFrame.Save(filePath, encoder, encoderParams);

            // ImageCodecInfo 객체와 EncoderParameter 객체 배열을 해제합니다.
            encoderParams.Dispose();
            // ImageCodecInfo 객체는 Dispose() 메서드를 호출하지 않아도 됩니다.
            showMessageDelegate("이미지 촬영이 완료 되었습니다.\r\n");

            return true;

        }

        // ImageFormat에 따른 ImageCodecInfo 객체를 반환합니다.
        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        // ImageFormat에 따른 EncoderParameter 객체 배열을 반환합니다.
        private EncoderParameters GetEncoderParameters(ImageFormat format)
        {
            EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L); // 이미지 품질 설정

            return encoderParams;
        }

        public void clear()
        {
            if (FinalVideo != null && FinalVideo.IsRunning == true)
            {
                FinalVideo.Stop();
            }
        }
    }
}
