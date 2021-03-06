﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AaxToMp3GUI
{
    public partial class Form1 : Form
    {
        string filename = "";
        Converter converter = new Converter();
        public string Filename
        {
            get { return filename; }
            set { 
                filename = value;
                if (value == null)
                    value = "No file selected.";
            Invoke((MethodInvoker)delegate { 
                this.label1.Text = Path.GetFileName(value);
                this.button2.Enabled = filename != null;
            });

            }
        }
        public Form1()
        {
            InitializeComponent();
            openFileDialog1.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Audible\Programs\Downloads");
            converter.OnConvertingFinish += new EventHandler(converter_OnConvertingFinish);
            converter.OnNewConvertingState += new EventHandler(converter_OnNewConvertingState);
        }
        private string FormatKilobytes(float kilobytes)
        {
            string[] sizes = new string[] { "KB", "MB", "GB", "TB" };
            int i = 0;
            while (kilobytes>=1024)
            {
                kilobytes /= 1024;
                i++;
            }
            return kilobytes.ToString("N2") + " " + sizes[i];
        }
        void converter_OnNewConvertingState(object sender, EventArgs e)
        {
            Converter converter = (Converter)sender;
            ConvertingState cs = converter.ConvertingState;
            try
            {
                Invoke((MethodInvoker)delegate { label2.Text = 
                    "Duration:   " + cs.TimeSpan.ToString() + 
                  "\nSize:   " + FormatKilobytes(cs.KiloBytes) + 
                  "\nBitrate: " + cs.Bitrate; });
            }
            catch (Exception){}
           
        }

        void converter_OnConvertingFinish(object sender, EventArgs e)
        {
            try
            {
                Invoke((MethodInvoker)delegate { 
                label2.Text = "Conversion finished."; 
                button1.Enabled = button2.Enabled = true; });
            }
            catch (Exception) { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                Filename = openFileDialog1.FileName;              
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string outputFile = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".mp3");
            if (File.Exists(outputFile))
            {
                if (MessageBox.Show("The file " + Path.GetFileName(outputFile) + " already exists.\nOverwrite the file?", "File already exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                    File.Delete(outputFile);
            }
            button1.Enabled = button2.Enabled = false;
            converter.Start(Filename, outputFile);

        }

    }
}
