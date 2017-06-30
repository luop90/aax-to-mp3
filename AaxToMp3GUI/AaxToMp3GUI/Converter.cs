using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AaxToMp3GUI
{
    struct ConvertingState
    {
        public float Bitrate{get;set;}
        public long KiloBytes{get;set;}
        public TimeSpan TimeSpan { get; set; }

    }
    class Converter
    {
        Process aaxProcess;
        Process ffmpegProcess;
        ConvertingState convertingState;
        public event EventHandler OnNewConvertingState;
        public event EventHandler OnConvertingFinish;
        public ConvertingState ConvertingState
        {
            get { return convertingState; }
            private set
            {
                convertingState = value;
                if (OnNewConvertingState != null)
                    OnNewConvertingState(this, new EventArgs());
            }
        }
        public Converter()
        {
            
        }
        private void Init()
        {
            aaxProcess = new Process();
            ffmpegProcess = new Process();
            string path = Application.StartupPath;
            string aaxToMp3 = "AaxToMp3.exe";
            string ffmpeg = "ffmpeg.exe";
            ProcessStartInfo aaxProcessInfo = new ProcessStartInfo();
            aaxProcessInfo.FileName = aaxToMp3;
            aaxProcessInfo.UseShellExecute = false;
            aaxProcessInfo.RedirectStandardOutput = true;
            aaxProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
            aaxProcessInfo.CreateNoWindow = true;
            aaxProcess.StartInfo = aaxProcessInfo;
            

            ProcessStartInfo ffmpegProcessInfo = new ProcessStartInfo();
            ffmpegProcessInfo.CreateNoWindow = true;
            ffmpegProcessInfo.FileName = ffmpeg;
            ffmpegProcessInfo.UseShellExecute = false;
            ffmpegProcessInfo.RedirectStandardInput = true;
            ffmpegProcessInfo.RedirectStandardError = true;
            ffmpegProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ffmpegProcess.StartInfo = ffmpegProcessInfo;
            ffmpegProcess.ErrorDataReceived += new DataReceivedEventHandler(ffmpegProcess_ErrorDataReceived);
        }
        public void Start(string inputFile, string outputFile)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                Init();
                aaxProcess.StartInfo.Arguments = " -i \"" + inputFile + "\"";
                ffmpegProcess.StartInfo.Arguments = " -i pipe:0 \"" + outputFile + "\"";


                using (aaxProcess)
                {
                    aaxProcess.Start();
                    using (ffmpegProcess)
                    {
                        ffmpegProcess.Start();
                        ffmpegProcess.BeginErrorReadLine();
                        using (StreamReader reader = aaxProcess.StandardOutput)
                        {
                            using (StreamWriter writer = ffmpegProcess.StandardInput)
                            {
                                char[] buffer = new char[4096];
                                while (!reader.EndOfStream)
                                {
                                    reader.Read(buffer, 0, buffer.Length);
                                    writer.Write(buffer);
                                }
                            }
                        }
                    }
                    aaxProcess.WaitForExit();
                    if (OnConvertingFinish != null)
                        OnConvertingFinish(this, new EventArgs());
                }
            });
        }

        void ffmpegProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Regex regex = new Regex(@"size\=[\s]*([0-9]+)kB time\=([0-9]{2}:[0-9]{2}:[0-9]{2}\.[0-9]{2}) bitrate\= (.*)kbits/s");
            if(e.Data != null)
            if (regex.IsMatch(e.Data))
            {
                Match m = regex.Match(e.Data);
                long kB = Convert.ToInt64(m.Groups[1].Value);
                string[] tsArr = m.Groups[2].Value.Split(new char[] { ':', '.' });
                TimeSpan ts = new TimeSpan(0, Convert.ToInt32(tsArr[0]), Convert.ToInt32(tsArr[1]), Convert.ToInt32(tsArr[2]), Convert.ToInt32(tsArr[3]) * 10);
                float bitrate = Convert.ToSingle(m.Groups[3].Value, new CultureInfo("en"));
                ConvertingState = new ConvertingState() { Bitrate = bitrate, KiloBytes = kB, TimeSpan = ts };
            }
        }
    }
}
