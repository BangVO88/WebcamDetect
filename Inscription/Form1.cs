﻿/**
 * @Author: Kheir Eddine FARFAR
 * @Author Github: https://github.com/Reddine
 * @Description: Capture Images from WebCam
 */

using Accord.Video.FFMPEG;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using WebCam;

//
//From https://github.com/Reddine/Webcam-Capture-AForge.Net
//
//
namespace Inscription
{
    public partial class Form1 : Form
    {
        private long tickCounter = 0;
        private const string format = "yyyy_MM_dd_HH_mm_ss_fff";

        private FilterInfoCollection videoDevices;
        private readonly MotionDetector detector = new MotionDetector(new TwoFramesDifferenceDetector(), null);
        private readonly List<Bitmap> currentFrames = new List<Bitmap>();
        private readonly List<float> motionLevels = new List<float>();

        public Form1()
        {
            InitializeComponent();

            try
            {
                /// List of webcams
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();

                /// Fill list of devices
                foreach (FilterInfo device in videoDevices)
                    devicesCombo.Items.Add(device.Name);
            }
            catch (Exception)
            {
                devicesCombo.Items.Add("No local capture devices");
                devicesCombo.Enabled = false;
                takePictureBtn.Enabled = false;
            }
            finally
            {
                devicesCombo.SelectedIndex = 0;
            }
        }

        private void takePictureBtn_Click(object sender, EventArgs e)
        {
            DateTime time = DateTime.Now;
            string format = "yyyy_MM_dd_HH_mm_ss";
            string strFilename = "Capture-" + time.ToString(format) + ".jpg";
            if (videoSourcePlayer.IsRunning)
            {
                Bitmap picture = videoSourcePlayer.GetCurrentVideoFrame();
                picture.Save(strFilename, ImageFormat.Jpeg);
                currentFrames.Add(picture);
                MessageBox.Show($"Кадр сохранён: {strFilename}", "Оповещение");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            videoSourcePlayer.SignalToStop();
            videoSourcePlayer.WaitForStop();
            videoDevices = null;
            videoDevices = null;
        }

        private void devicesCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tickCounter != 0)            
                RestartStreamCam();           
        }

        #region Start/Stop/Restart camera
        private void RestartStreamCam()
        {
            StopStreamCam();
            StartStreamCam();
        }

        private void StopStreamCam()
        {
            videoSourcePlayer.SignalToStop();
            videoSourcePlayer.WaitForStop();
        }

        private void StartStreamCam()
        {
            videoSourcePlayer.VideoSource = new VideoCaptureDevice(videoDevices[devicesCombo.SelectedIndex].MonikerString);
            videoSourcePlayer.Start();
        }
        #endregion

        private void ButtonStart_Click(object sender, EventArgs e)
        {
            RestartStreamCam();
            timer.Start();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            StopStreamCam();
            timer.Stop();
        }

        private void CreateVideo()
        {
            Image fFrame = currentFrames.First();
            Size size = new Size(fFrame.Width, fFrame.Height);

            using (VideoFileWriter vw = new VideoFileWriter())
            {
                string strFilename = $"Video_{DateTime.Now.ToString(format)}.avi";
                vw.Open(strFilename, size.Width, size.Height, 10, VideoCodec.MPEG4);

                /// Create video frames
                foreach (var img in currentFrames)
                    vw.WriteVideoFrame(img);

                vw.Close();
            }
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (tickCounter > 0 && tickCounter % 600 == 0)
            {
                /// Avg motion on 1 minutes
                float index = AverageMotion(60);
                float max = MaxMotion(60);
                //label2.Text = $"index:{Math.Round(index, 5)} max: {Math.Round(max, 5)}";
                if (index > 0.07)
                    CreateVideo();
            }
            
            if (videoSourcePlayer.IsRunning)
            {
                /// Take a picture 1 time in half a second
                Bitmap picture = videoSourcePlayer.GetCurrentVideoFrame();
                if (picture != null)
                {
                    float diff = detector.ProcessFrame(picture);
                    currentFrames.Add(picture);
                    motionLevels.Add(diff);
                }
            }

            //labelTicks.Text = $"{tickCounter++}";
        }

        public float AverageMotion(int seconds)
        {
            float num1 = 0.0f;
            int num2 = motionLevels.Count - 1 - seconds;

            if (num2 < 0)
                num2 = 0;

            for (int index = motionLevels.Count - 1; index > num2; --index)
                num1 += motionLevels[index];

            return num1 / seconds;
        }

        public float MaxMotion(int seconds)
        {
            float max = -10;
            int num2 = motionLevels.Count - 1 - seconds;
            if (num2 < 0)
                num2 = 0;

            for (int index = motionLevels.Count - 1; index > num2; --index)
                if (motionLevels[index] > max)
                    max = motionLevels[index];

            return max;
        }

        private void ButtonVideo_Click(object sender, EventArgs e)
        {
            CreateVideo();
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void changeAccount_Click(object sender, EventArgs e)
        {
            new AccountSettings().ShowDialog();
        }
    }
}