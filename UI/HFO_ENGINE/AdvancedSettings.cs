﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HFO_ENGINE
{
    public partial class AdvancedSettings : Form
    {
        public AdvancedSettings(string host, string port, string log_file, string trc_temp_path)
        {
            InitializeComponent();
            Hostname_txt.Text = host;
            Port_txt.Text = port;
            Logfile_txt.Text = log_file;
            TrcTemp_txt.Text = trc_temp_path;
        }

        private void AdvancedSettings_save_btn_Click(object sender, EventArgs e)
        {
            Program.Controller.SetAdvancedSettings(Hostname_txt.Text, Port_txt.Text,
                                                   Logfile_txt.Text, TrcTemp_txt.Text);
        }

        private void Test_btn_Click(object sender, EventArgs e)
        {
            Program.Controller.TestConnection();
        }
    }
}
