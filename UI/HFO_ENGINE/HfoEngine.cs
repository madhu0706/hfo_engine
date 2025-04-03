using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using CommandLine;
using System.Net;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management;
using System.Text.Json;
using HFO_ENGINE;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace HFO_ENGINE
{
    static class Program
    {
        public static Controller Controller { get; set; }
        public static string MainDir()
        {
            return AppContext.BaseDirectory.Replace("\\", "/");
        }

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Controller = new Controller();
            string detectedTrcFile = Controller.DetectOpenedTrcFile();
            Controller.SetTrcFile(detectedTrcFile); // Using the synchronous method

            Controller.Init(args);
        }

    }
    class Controller
    {
        //Constructor
        public Controller()
        {
            this.Model = new Model();
        }
        private Model Model { get; set; }

        public void Init(string[] args)
        {
            //Command line parsing
            Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
            .WithNotParsed<Options>((errs) => HandleParseError(errs));
            this.TestDefaultConnection();
            Application.Run(this.GetScreen_Home());
        }
        //Getters of the model
        public API GetAPI() { return this.Model.API; }
        public AnalizerParams GetAnalizerParams() { return this.Model.AnalizerParams; }
        public ConversionParams GetConvParams() { return this.Model.ConversionParams; }
        public MainWindow GetScreen_Home() { return this.Model.HomeScreen; }
        public EEG GetScreen_EEG() { return this.Model.EEGScreen; }
        public Montage GetScreen_Montage() { return this.Model.MontageScreen; }
        public TimeWindow GetScreen_TimeWindow() { return this.Model.TimeWindowScreen; }
        public CycleTime GetScreen_CycleTime() { return this.Model.CycleTimeScreen; }
        public EVT GetScreen_Evt() { return this.Model.EvtScreen; }
        public AdvancedSettings GetScreen_Settings() { return this.Model.SettingsScreen; }
        public Progress GetScreen_AnalizerProgress() { return this.Model.ProgressScreen; }
        public ConversorStep1 GetScreen_Conv_1() { return this.Model.ConvScreen_1; }
#pragma warning disable CS8603 // Possible null reference return.
        public ConversorStep2 GetScreen_Conv_2() { return this.Model.ConvScreen_2; }
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning disable CS8603 // Possible null reference return.
        public ConversorStep3 GetScreen_Conv_3() { return this.Model.ConvScreen_3; }
#pragma warning restore CS8603 // Possible null reference return.

        public int GetTrcDuration() { return this.Model.TrcDuration; }
        public string[] GetMontageNames() { return this.Model.MontageNames; }
        public string GetLogFile() { return this.Model.LogFile; }
        public string GetTrcTempDir() { return this.Model.TRCTempDir; }


        public void SetTrcFile(string trc_fname, bool isManualOverride = false)
        {
            if (this.IsBusy())
            {
                this.UnavailableOptionMsg();
                return;
            }

            // If no TRC file is provided, try to detect the opened TRC file from Brain Quick.
            if (string.IsNullOrEmpty(trc_fname))
            {
                trc_fname = DetectOpenedTrcFile();
                this.Log($"Detected TRC file from Brain Quick: {trc_fname}");
            }

            // Set the TRC file path and log.
            this.Model.AnalizerParams.TrcFile = trc_fname;
            this.Log($"TRC file set: {trc_fname}");

            // Upload the TRC file and get metadata synchronously.
            try
            {
                UploadTrcFile_And_GetMetadata(trc_fname);  // Synchronous version of UploadTrcFile_And_GetMetadataAsync
                this.Log("Uploaded TRC file successfully.");
            }
            catch (Exception ex)
            {
                this.Log($"Error in SetTrcFile: {ex.Message}");
            }
        }
        public void SetMontages(string sug_montage, string bp_montage)
        {
            if (this.IsBusy()) this.UnavailableOptionMsg();
            else
            {
                this.Model.AnalizerParams.SuggestedMontage = sug_montage;
                this.Model.AnalizerParams.BipolarMontage = bp_montage;
                this.Log(String.Format("Montages setted to Suggested: {0} and Bipolar: {1}", sug_montage, bp_montage));

            }
        }
        public void SetTimeWindow(int start_time, int stop_time)
        {
            if (this.IsBusy()) this.UnavailableOptionMsg();
            else
            {
                this.Model.AnalizerParams.StartTime = start_time;
                this.Model.AnalizerParams.StopTime = stop_time;
                this.Log(String.Format("Time-window setted to [{0} , {1})  ", start_time, stop_time));

            }
        }
        public void SetCycleTime(bool parallel_flag, int cycle_time)
        {
            if (this.IsBusy()) this.UnavailableOptionMsg();
            else
            {
                if (parallel_flag) this.Model.AnalizerParams.CycleTime = cycle_time;
                else this.Model.AnalizerParams.CycleTime = GetAnalizerParams().StopTime - GetAnalizerParams().StartTime;
                this.Log(String.Format("Cycletime setted"));
            }
        }
        public void SetEvtFile(string evt_dir, string evt_fname)
        {
            if (this.IsBusy()) this.UnavailableOptionMsg();
            else
            {
                this.Model.AnalizerParams.EvtFile = evt_dir + "/" + evt_fname;
                Console.WriteLine("Setting evt name to {0} ", Model.AnalizerParams.EvtFile);
                this.Log(String.Format("Evt saving path setted to {0}", Model.AnalizerParams.EvtFile));
            }
        }
        public void SetAdvancedSettings(string hostname, string port, string log_file, string trc_temp_dir)
        {
            if (this.IsBusy()) this.UnavailableOptionMsg();
            else
            {
                this.Model.API.Hostname = hostname;
                this.Model.API.Port = port;
                this.Model.LogFile = log_file;
                this.Model.TRCTempDir = trc_temp_dir;
                this.Log(String.Format("Setting advanced parameters {0} {1}, {2}, {3}",
                        hostname, port, log_file, trc_temp_dir));

            }
        }
        private static readonly HttpClient httpClient = new HttpClient(); // Global instance

        private void UploadTrcFile_And_GetMetadata(string trc_fname)
        {
            string tempTrcPath = this.GetTRCTempPath(trc_fname);
            this.Log($"Starting upload for TRC file: {trc_fname}, Temp Path: {tempTrcPath}");

            try
            {
                if (!File.Exists(trc_fname))
                {
                    this.Log($"Error: TRC file does not exist. File path: {trc_fname}");
                    return; 
                }

                try
                {
                    File.Copy(trc_fname, tempTrcPath, true);
                }
                catch (Exception ex)
                {
                    this.Log($"File copy failed: {ex.Message}");
                    MessageBox.Show($"File copy failed: {ex.Message}");
                    return;
                }

                var eegScreen = Program.Controller.GetScreen_EEG();
                if (eegScreen == null)
                {
                    this.Log("Error: EEG screen is null.");
                    MessageBox.Show("Error: EEG screen is null.");
                    return;
                }
                eegScreen.Invoke((MethodInvoker)delegate {
                    eegScreen.UpdateProgressDesc("Uploading TRC to the server...");
                });

                string uri_upload = this.GetAPI().GetUri_UploadTRC();
                this.Log($"Initiating upload to: {uri_upload}");

                using (var fileStream = File.OpenRead(tempTrcPath))
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    content.Add(fileContent, "file", Path.GetFileName(tempTrcPath));

                    var response = httpClient.PostAsync(uri_upload, content).GetAwaiter().GetResult();

                    if (response.IsSuccessStatusCode)
                    {
                        this.Log("Upload TRC was completed.");
                        eegScreen.Invoke((MethodInvoker)delegate {
                            eegScreen.UpdateProgressDesc("Setting TRC metadata...");
                            eegScreen.UploadProgress = 20;
                        });

                        Program.Controller.SetTrcMetadata(trc_fname);

                        eegScreen.Invoke((MethodInvoker)delegate {
                            eegScreen.UpdateProgressDesc("");
                            eegScreen.UploadProgress = 0;
                        });
                    }
                    else
                    {
                        this.Log($"Error during file upload: {response.ReasonPhrase}");
                        MessageBox.Show($"Upload failed: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log($"Error during file upload: {ex.Message}");
                MessageBox.Show($"Error during TRC upload: {ex.Message}");
            }
        }
        private string GetCommandLine(Process process)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // Log the process name and ID for debugging
                    this.Log($"Process Name: {process.ProcessName}, Process ID: {process.Id}");

                    using (var searcher = new ManagementObjectSearcher(
                        $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                    {
                        foreach (ManagementObject processObj in searcher.Get())
                        {
                            // Log the command line for debugging
                            string commandLine = processObj["CommandLine"]?.ToString() ?? string.Empty; ;
                            this.Log($"Command Line: {commandLine}");
                            return commandLine;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log any error for debugging
                    this.Log($"Error retrieving command line: {ex.Message}");
                }
                return string.Empty; 
            }
            else
            {
                this.Log("Command line retrieval using System.Management is not supported on this OS.");
                return string.Empty; 
            }
        }


        private void SetTrcMetadata(string trc_fname)
        {
            this.Log("Setting TRC metadata");
            string uri_trc_info = this.GetAPI().GetUri_TrcInfo(Path.GetFileName(trc_fname));
            string json_resp = GetJsonSync(uri_trc_info);
            TRCInfo trc_info = JsonConvert.DeserializeObject<TRCInfo>(json_resp);
            this.Model.MontageNames = trc_info.montage_names;
            this.GetScreen_Montage().LoadMontages(trc_info.montage_names);
            this.Model.TrcDuration = trc_info.recording_len_snds;
            this.GetScreen_TimeWindow().SetTRCDuration(trc_info.recording_len_snds);
        }
        public bool IsMetadataSetted() { return this.GetTrcDuration() != 0; }
        public void RunHFOAnalizer()
        {
            this.Log("Starting TRC analysis.");
            this.Model.IsAnalizing = true;
            this.GetScreen_Home().AbrirFormHija(this.GetScreen_AnalizerProgress());
            this.GetScreen_AnalizerProgress().StartTimer();

            Thread hfo_analisis = new Thread(() =>
            {
                this.GetScreen_AnalizerProgress().UpdateProgressSafe(1);
                this.GetScreen_AnalizerProgress().UpdateProgressDescSafe("Analizing TRC in remote server...");
                this.AnalizeWith(this.GetAnalizerParams());
                this.GetScreen_AnalizerProgress().SaveAndReset_timer_Safe();

                this.GetScreen_AnalizerProgress().UpdateProgressSafe(1);
                this.GetScreen_AnalizerProgress().UpdateProgressDescSafe("Downloading detected events as evt file...");
                string remote_evt_fname = Path.GetFileNameWithoutExtension(GetAnalizerParams().TrcFile) + ".evt";
                this.DownloadEvt(remote_evt_fname, GetAnalizerParams().EvtFile);
                this.GetScreen_AnalizerProgress().UpdateProgressSafe(100);
                this.GetScreen_AnalizerProgress().UpdateProgressDescSafe("Evt was downloaded.");

                MessageBox.Show("Analysis Completed. Please close the HFO_ENGINE."); 
                this.GetScreen_AnalizerProgress().UpdateProgressSafe(0);
                this.GetScreen_AnalizerProgress().UpdateProgressDescSafe("");

                this.Model.IsAnalizing = false;
                this.Log("Analysis thread finished");
            }
            );
            hfo_analisis.Start();
        }
        private void AnalizeWith(AnalizerParams args)
        {
            //Analizer call
            string uri_run = this.GetAPI().GetUri_Analizer();
            Dictionary<string, string> Params = new Dictionary<string, string>
            {
                { "trc_fname", Path.GetFileName(args.TrcFile) },
                { "str_time", args.StartTime.ToString() },
                { "stp_time", args.StopTime.ToString() },
                { "cycle_time", args.CycleTime.ToString() },
                { "sug_montage", args.SuggestedMontage },
                { "bp_montage", args.BipolarMontage }
            };
            string serialized_params = JsonConvert.SerializeObject(Params, new KeyValuePairConverter());

            this.Log(String.Format("Analysis request with params: {0},{1},{2},{3},{4},{5}",
                            Params["trc_fname"], Params["str_time"], Params["stp_time"],
                            Params["cycle_time"], Params["sug_montage"], Params["bp_montage"]));
            string run_response_str = this.PostJsonSync(uri_run, serialized_params);
            JsonNode run_response = JsonNode.Parse(run_response_str);

            if (run_response != null && run_response["error_msg"] != null)
            {
                MessageBox.Show(run_response["error_msg"].ToString());
            }
            else
            {
                string pid = run_response["task_id"].ToString();
                string uri_task_state = this.GetAPI().GetUri_JobState(pid);
                int progress = 0;
                do
                {
                    string task_state_string = this.GetJsonSync(uri_task_state);
                    try
                    {
                        JsonNode task_state = JsonNode.Parse(task_state_string);
                        if (task_state != null && task_state["progress"] != null)
                        {
#pragma warning disable CS8604 // Possible null reference argument.
                            progress = (int)task_state["progress"];
#pragma warning restore CS8604 // Possible null reference argument.
                            this.GetScreen_AnalizerProgress().UpdateProgressSafe(progress);
                        }
                        else
                        {
                            this.Log("Warning: Malformed json response from task state api.");
                        }

                    }
                    catch (System.Text.Json.JsonException ex)
                    {
                        this.Log($"Error parsing JSON: {ex.Message}");
                        progress = 100;
                    }

                    Thread.Sleep(1000);
                } while (progress < 100);
                this.Log("Analyzer has finished remotely.");
            }
        }
        public bool IsAnalizing() { return this.Model.IsAnalizing; }
        public void StartEdfConversion(string edf_fname)
        {
            this.Model.IsConverting = true;
            this.GetScreen_Conv_1().UpdateProgressDescSafe("Uploading EDF to the server...");

            string uri_upload = this.GetAPI().GetUri_UploadEdf();

            try
            {
                using (HttpClient client = new HttpClient())
                using (var fileStream = File.OpenRead(edf_fname))
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    content.Add(fileContent, "file", Path.GetFileName(edf_fname));

                    // Upload EDF file to server synchronously
                    HttpResponseMessage response = client.PostAsync(uri_upload, content).Result;  // Synchronous call

                    if (response.IsSuccessStatusCode)
                    {
                        this.GetScreen_Conv_1().UpdateProgressDescSafe("Getting suggested channel name mapping...");
                        this.GetScreen_Conv_1().UpdateProgressSafe(15);

                        // Simulating file processing steps
                        Dictionary<string, string> suggested_mapping = this.GetChMapping(edf_fname);
                        this.GetScreen_Conv_1().UpdateProgressSafe(50);

                        this.Model.ConversionParams = new ConversionParams(Path.GetFileName(edf_fname), suggested_mapping);
                        this.GetScreen_Conv_1().UpdateProgressSafe(65);

                        // Go to confirm translations screen
                        this.Model.ConvScreen_2 = new ConversorStep2(suggested_mapping);
                        this.GetScreen_Conv_1().UpdateProgressSafe(100);
                        this.GetScreen_Conv_1().UpdateProgressDescSafe("");
                        this.GetScreen_Conv_1().UpdateProgressSafe(0);
                        this.GetScreen_Home().AbrirFormHija(this.GetScreen_Conv_2());
                    }
                    else
                    {
                        Console.WriteLine($"Error during file upload: {response.ReasonPhrase}");
                        this.GetScreen_Conv_1().UpdateProgressDescSafe($"Upload failed: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during EDF upload: {ex.Message}");
                this.GetScreen_Conv_1().UpdateProgressDescSafe($"Error: {ex.Message}");
            }
        }
        private Dictionary<string, string> GetChMapping(string edf_fname)
        {
            string uri_suggestion = this.GetAPI().GetUri_Suggested_ChName_Translation(Path.GetFileName(edf_fname));
            string suggestion_response = this.GetJsonSync(uri_suggestion);
            SuggestionResponse suggestion_response_dict = JsonConvert.DeserializeObject<SuggestionResponse>(suggestion_response);
            return suggestion_response_dict.suggested_mapping;
        }
        public void ConfirmChMapping(Dictionary<string, string> ch_translations)
        {
            this.Model.ConversionParams.ch_names_mapping = ch_translations;
            this.Model.ConvScreen_3 = new ConversorStep3();
            this.GetScreen_Home().AbrirFormHija(this.GetScreen_Conv_3());
        }
        public void ConvertEdf(string trc_saving_dir)
        {
            this.GetScreen_Conv_3().UpdateProgressDescSafe("Performing conversion in remote server...");
            this.GetScreen_Conv_3().UpdateProgressSafe(5);

            Thread conversion = new Thread(() => {
                string uri_edf_to_trc = this.GetAPI().GetUri_Converter();
                string serialized_conv_params = JsonConvert.SerializeObject(this.GetConvParams());
                string run_response_str = this.PostJsonSync(uri_edf_to_trc, serialized_conv_params);
                JObject run_response = JObject.Parse(run_response_str);

                if (run_response.ContainsKey("error_msg"))
                {
                    MessageBox.Show(run_response["error_msg"].ToString());
                }
                else
                {
                    string pid = run_response["task_id"].ToString();
                    string uri_task_state = this.GetAPI().GetUri_JobState(pid);
                    int progress = 0;
                    do
                    {
                        string task_state_string = this.GetJsonSync(uri_task_state);
                        try
                        {
                            JsonNode task_state = JsonNode.Parse(task_state_string);

                            if (task_state != null && task_state["progress"] != null)
                            {
#pragma warning disable CS8604 // Possible null reference argument.
                                progress = (int)task_state["progress"];
#pragma warning restore CS8604 // Possible null reference argument.
                                this.GetScreen_Conv_3().UpdateProgressSafe(progress);
                            }
                            else
                            {
                                this.Log("Warning: malformed json response for task state.");
                                progress = 100; //prevent infinite loop.
                            }

                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            this.Log($"Error parsing JSON: {ex.Message}");
                            progress = 100; //prevent infinite loop.
                        }

                        Thread.Sleep(1000);
                    } while (progress < 100);
                    this.Log("Conversion completed.");

                    string basename = Path.GetFileNameWithoutExtension(GetConvParams().edf_fname);
                    string remote_trc_fname = basename + ".TRC";
                    string trc_saving_path = trc_saving_dir + remote_trc_fname;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    this.DownloadTRC(remote_trc_fname, trc_saving_path);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                this.Model.IsConverting = false;
            }
            );
            conversion.Start();
        }
        private static readonly HttpClient client = new HttpClient();

        private async Task DownloadTRC(string remote_trc_fname, string trc_saving_path)
        {
            this.GetScreen_Conv_3().UpdateProgressSafe(0);
            this.GetScreen_Conv_3().UpdateProgressDescSafe("Downloading converted TRC from remote server...");

            string uri_download_trc = this.GetAPI().GetUri_DownloadTRC(remote_trc_fname);
            this.Log($"Downloading TRC from: {uri_download_trc}");

            try
            {
                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(uri_download_trc, HttpCompletionOption.ResponseHeadersRead)) // Asynchronous call
                {
                    response.EnsureSuccessStatusCode(); 

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var downloadedBytes = 0L;

                    using (var contentStream = await response.Content.ReadAsStreamAsync()) // Asynchronous read
                    using (var fileStream = new FileStream(trc_saving_path, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) // Asynchronous read
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead); // Asynchronous write
                            downloadedBytes += bytesRead;

                            int progress = totalBytes > 0 ? (int)((downloadedBytes * 100) / totalBytes) : -1;

                            Console.WriteLine($"Downloading converted TRC {progress}% complete...");
                            this.GetScreen_Conv_3().UpdateProgressSafe(progress);
                        }
                    }

                    this.GetScreen_Conv_3().UpdateProgressSafe(100);
                    this.GetScreen_Conv_3().UpdateProgressDescSafe("Download has finished.");
                    this.Log("Converted TRC download is complete.");
                }
            }
            catch (Exception ex)
            {
                this.Log($"Error downloading TRC: {ex.Message}");
                this.GetScreen_Conv_3().UpdateProgressDescSafe($"Error: {ex.Message}");
            }
        }
        public bool IsConverting() { return this.Model.IsConverting; }
        public void SetConvFlag(bool flag) { this.Model.IsConverting = flag; }
        public void TestConnection()
        {

            HttpClient httpClient = new HttpClient();
            string index_uri = Program.Controller.GetAPI().URI();
            try
            {
                HttpResponseMessage response = httpClient.GetAsync(index_uri).Result;
                response.EnsureSuccessStatusCode();    // Throw if not a success code.
                string json_index_str = response.Content.ReadAsStringAsync().Result;
                JsonObject json_index = (JsonObject)JsonValue.Parse(json_index_str);
                MessageBox.Show(json_index["message"] + ". Server is up!");
            }
            catch (Exception)
            {
                MessageBox.Show("Server is down. Please connect to the server.");
            }
        }
        public void TestDefaultConnection()
        {
            MessageBox.Show("HFO Engine is ready to use.");
        }
        private void DownloadEvt(string remote_evt_fname, string dest)
        {
            DownloadEvtSync(remote_evt_fname, dest);
        }

        private void RunOptionsAndReturnExitCode(Options opts)
        {
            if (!string.IsNullOrEmpty(opts.TrcFile))
            {
                this.Model.AnalizerParams.TrcFile = (opts.TrcFile).Replace("\\", "/");
                Console.WriteLine("Input Trc name {0} ", GetAnalizerParams().TrcFile);
                this.GetScreen_EEG().SetTrcFile(GetAnalizerParams().TrcFile);
            }
            if (!string.IsNullOrEmpty(opts.EvtFile))
            {
                this.Model.AnalizerParams.EvtFile = (opts.EvtFile).Replace("\\", "/");
                Console.WriteLine("Input evt name {0} ", GetAnalizerParams().EvtFile);
                this.GetScreen_Evt().SetEvtFile(GetAnalizerParams().EvtFile);
            }
        }
        private void HandleParseError(IEnumerable<Error> errs)
        {
            //TODO
            throw new NotImplementedException();
        }
        private string GetTRCTempPath(string trc_fname) { return this.GetTrcTempDir() + Path.GetFileName(trc_fname); }
        private void UnavailableOptionMsg()
        {
            MessageBox.Show("This option is unavailable at this moment. Please try again later.");
        }
        public bool IsBusy() { return IsConverting() || IsAnalizing(); }

        private string GetJsonSync(string uri)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.GetAsync(uri).Result;

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Unsuccesfull status code from server: " + response.StatusCode.ToString());
            }
            return response.Content.ReadAsStringAsync().Result;
        }
        private string PostJsonSync(string uri, string serialized_json)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                StringContent body = new StringContent(serialized_json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = httpClient.PostAsync(uri, body).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }
        private void Log(string info)
        {
            bool log_activated = true;
            if (log_activated)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(this.GetLogFile(), true))
                {
                    file.WriteLine(info);
                }
            }
        }

        public string DetectOpenedTrcFile()
        {
            string trcFilePath = null;

            this.Log("Starting TRC file detection...");

            foreach (var process in Process.GetProcessesByName("BrainQuickAgent"))
            {
                try
                {
                    string commandLine = GetCommandLine(process);
                    this.Log($"BrainQuickAgent Command Line: {commandLine}");

                    if (commandLine.IndexOf(".TRC", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        string[] args = commandLine.Split(' ');

                        foreach (string arg in args)
                        {
                            if (arg.EndsWith(".TRC", StringComparison.OrdinalIgnoreCase))
                            {
                                trcFilePath = arg;
                                this.Log($"Detected TRC file: {trcFilePath}");
                                break;
                            }
                        }
                    }
                    else
                    {
                        this.Log("No TRC file reference found in BrainQuickAgent command line.");
                    }
                }
                catch (Exception ex)
                {
                    this.Log($"Error detecting TRC file: {ex.Message}");
                }
            }

            if (string.IsNullOrEmpty(trcFilePath))
            {
                this.Log("Final Result: No TRC file detected.");
            }
            else
            {
                this.Log($"Final Result: Detected TRC file: {trcFilePath}");
            }

            return trcFilePath ?? string.Empty; 
        }

        private void BtnUploadManual_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "TRC Files|*.trc";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedTrcFile = openFileDialog.FileName;
                    this.SetTrcFile(selectedTrcFile, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void DownloadEvtSync(string remote_evt_fname, string dest)
        {
            this.Log("Downloading EVT file...");
            string uri_get_evt = this.GetAPI().GetUri_DownloadEvt(remote_evt_fname);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.GetAsync(uri_get_evt).Result;  

                    if (response.IsSuccessStatusCode)
                    {
                        using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            response.Content.CopyTo(fs, null, CancellationToken.None);
                        }
                        this.Log("Download completed.");
                    }
                    else
                    {
                        this.Log($"Error downloading EVT file: {response.ReasonPhrase}");
                    }
                }
            }
            catch (Exception ex)
            {
                this.Log($"Error during EVT file download: {ex.Message}");
            }
        }


    }

    class Model
    {
      
        public Model()
        {
            this.API = new API("Enter Host IP Address", "8080");
            this.AnalizerParams = new AnalizerParams();
#pragma warning disable CS8625
            this.ConversionParams = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            this.TrcDuration = 0;
            this.MontageNames = new string[] { };
            this.LogFile = Program.MainDir() + "logs/ez_detect_gui_log.txt";
            this.TRCTempDir = Program.MainDir() + "temp/";

            //Build screens
            this.HomeScreen = new MainWindow();
            this.EEGScreen = new EEG();
            this.MontageScreen = new Montage();
            this.TimeWindowScreen = new TimeWindow();
            this.CycleTimeScreen = new CycleTime();
            this.EvtScreen = new EVT();
            this.SettingsScreen = new AdvancedSettings(this.API.Hostname, this.API.Port, this.LogFile, this.TRCTempDir);
            this.ProgressScreen = new Progress();

            this.ConvScreen_1 = new ConversorStep1();
            this.ConvScreen_2 = null;
            this.ConvScreen_3 = null;

            this.IsAnalizing = false;
            this.IsConverting = false;

        }

        //Collaborators
        public API API { get; set; }
        public AnalizerParams AnalizerParams { get; set; }
        public ConversionParams ConversionParams { get; set; }

        public int TrcDuration { get; set; }
        public string[] MontageNames { get; set; }
        public string LogFile { get; set; }
        public string EvtDir { get; set; }
        public string TRCTempDir { get; set; }

        public MainWindow HomeScreen { get; set; }
        public EEG EEGScreen { get; set; }
        public Montage MontageScreen { get; set; }
        public TimeWindow TimeWindowScreen { get; set; }
        public CycleTime CycleTimeScreen { get; set; }
        public EVT EvtScreen { get; set; }
        public AdvancedSettings SettingsScreen { get; set; }
        public Progress ProgressScreen { get; set; }

        public ConversorStep1 ConvScreen_1 { get; set; }
        public ConversorStep2? ConvScreen_2 { get; set; }
        public ConversorStep3? ConvScreen_3 { get; set; }

        public bool IsAnalizing { get; set; }
        public bool IsConverting { get; set; }
    }

    class API
    {
        //Constructor
        public API(string hostname, string port)
        {
            this.Hostname = hostname;
            this.Port = port;
        }

        //Colaborators
        public string Hostname { get; set; }
        public string Port { get; set; }
        public string URI() { return "http://" + this.Hostname + ":" + this.Port; }
        public string GetUri_JobState(string job_id) { return this.URI() + "/task_state/" + job_id; }

        //Analizer URIs
        public string GetUri_AnalizerBP() { return this.URI() + "/analyzer"; }
        public string GetUri_UploadTRC() { return this.GetUri_AnalizerBP() + "/upload_trc"; }
        public string GetUri_TrcInfo(string trc_fname) { return this.GetUri_AnalizerBP() + "/trc_info/" + trc_fname; }
        public string GetUri_Analizer() { return this.GetUri_AnalizerBP() + "/analyze"; }
        public string GetUri_DownloadEvt(string evt_fname) { return this.GetUri_AnalizerBP() + "/download_evt/" + evt_fname; }

        //Converter URIs
        public string GetUri_ConverterBP() { return this.URI() + "/converter"; }
        public string GetUri_UploadEdf() { return this.GetUri_ConverterBP() + "/upload_edf"; }
        public string GetUri_Suggested_ChName_Translation(string edf_fname) { return this.GetUri_ConverterBP() + "/suggested_ch_name_mapping/" + edf_fname; }
        public string GetUri_Converter() { return this.GetUri_ConverterBP() + "/convert"; }
        public string GetUri_DownloadTRC(string trc_fname) { return this.GetUri_ConverterBP() + "/download_trc/" + trc_fname; }

    }

    class AnalizerParams
    {
        //Constructor
        public AnalizerParams()
        {
            this.TrcFile = String.Empty;
            this.EvtFile = String.Empty;
            this.StartTime = 0;
            this.StopTime = 0;
            this.CycleTime = 0;
            this.SuggestedMontage = String.Empty;
            this.BipolarMontage = String.Empty;
            this.TrcMetadata = String.Empty;
        }

        public string TrcFile { get; set; }
        public string EvtFile { get; set; }
        public int StartTime { get; set; }
        public int StopTime { get; set; }
        public int CycleTime { get; set; }
        public string SuggestedMontage { get; set; }
        public string BipolarMontage { get; set; }
        public string TrcMetadata { get; set; }
    }
    class ConversionParams
    {
        //Constructor
        public ConversionParams(string edf_filename, Dictionary<String, String> _suggested_mapping)
        {
            edf_fname = edf_filename;
            ch_names_mapping = _suggested_mapping;
        }

        //Colaborators
        public string edf_fname { get; set; }
        public Dictionary<string, string> ch_names_mapping { get; set; }

    }
    //Aux classes
    class Options
    {
        [Option('t', "trc", Required = false,
        HelpText = "Full path to input trc file to be processed.")]
        public string TrcFile { get; set; }

        [Option('x', "xml", Required = false,
         HelpText = "Full path to output evt file to be saved.")]
        public string EvtFile { get; set; }
        [Option(Default = false,
        HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }
    }
    class SuggestionResponse
    {
        public Dictionary<string, string> suggested_mapping { get; set; }
    }
    class TRCInfo
    {
        public string[] montage_names { get; set; }
        public int recording_len_snds { get; set; }
    }

}
