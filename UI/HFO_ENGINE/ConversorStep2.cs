﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HFO_ENGINE
{
    public partial class ConversorStep2 : Form
    {
        private int count { get; set; } = 0;

        public ConversorStep2( Dictionary<string, string> translations)
        {
            InitializeComponent();
            count = 0;
            
            foreach (var translation in translations)
            {
                Console.WriteLine(count.ToString()); //DEBUG

                TextBox LongName = new TextBox();
                LongName.Name = "LongNameTextBox_" + count.ToString();
                LongName.Size = LongNameTextBox.Size;
                LongName.Text = translation.Key;
                LongName.Enabled = false;

                TextBox ShortName = new TextBox();
                ShortName.Name = "ShortNameTextBox_" + count.ToString();
                ShortName.Size = ShortNameTextBox.Size;
                ShortName.Text = translation.Value;

                Panel translationPanel = new Panel();

                translationPanel.Name = "translationPanel_" + count.ToString();
                translationPanel.Controls.Add(LongName);
                translationPanel.Controls[LongName.Name].Location = LongNameTextBox.Location;

                translationPanel.Controls.Add(ShortName);
                translationPanel.Controls[ShortName.Name].Location = ShortNameTextBox.Location;

                PanelTranslations.Controls.Add(translationPanel);

                count = count + 1;
            }
            

        }

        private void Confirm_translations_btn_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> translations = new Dictionary<string, string>();
            HashSet<String> short_names = new HashSet<string>();

            foreach (Panel line in PanelTranslations.Controls)
            {
                int i = 0;
                string long_name= string.Empty;
                string short_name = string.Empty;
                foreach (TextBox ch_name_box in line.Controls)
                {
                    if (i == 0)
                    {
                        long_name = ch_name_box.Text;
                        i = 1;
                    }
                    else {
                        short_name = ch_name_box.Text;
                        if (String.IsNullOrEmpty(short_name) || short_name.Length > 5)
                        {
                            MessageBox.Show("TRC channel names must have between 1 and 5 characters of length.");
                            return;
                        }
                        if (!short_names.Add(short_name)) 
                        {
                            MessageBox.Show(String.Format("The translation {0} is repeated. Please correct and retry.", short_name));
                            return;
                        }
                        i = 0;
                    }
                }
                translations.Add(long_name, short_name);
            }

            Program.Controller.ConfirmChMapping(translations);
        }

        private void import_csv_btn_Click(object sender, EventArgs e)
        {
            string csv_fname = "";
            OpenFileDialog importcsvDialog = new OpenFileDialog
            {
                Title = "Browse csv",
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = "csv",
                Filter = "csv files(*.csv)| *.csv",
                FilterIndex = 2,
                RestoreDirectory = true,
            };
            if (importcsvDialog.ShowDialog() == DialogResult.OK)
            {
                csv_fname = importcsvDialog.FileName;
                using (var reader = new StreamReader(@csv_fname))
                {
                    int count = 0;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        try
                        {
                            TextBox edf_ch_name = Controls.Find("LongNameTextBox_" + count.ToString(), true).FirstOrDefault() as TextBox;
                            TextBox trc_ch_name = Controls.Find("ShortNameTextBox_" + count.ToString(), true).FirstOrDefault() as TextBox;
                            edf_ch_name.Text = values[0];
                            trc_ch_name.Text = values[1];
                            count++;
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show($"Csv is badly formed, each line should have format ----> EDF_CH_NAME , TRC_SHORT_NAME (where short is <=5 chars). Error: {err.Message}");
                            // Example of logging to a file (replace with your logging mechanism)
                            File.AppendAllText("error.log", $"{DateTime.Now}: {err}\n");
                        }
                    }   
                }
            }

        }
    }
}
