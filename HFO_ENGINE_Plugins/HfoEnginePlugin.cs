using Micromed.ExternalCalculation.Common;
using Micromed.ExternalCalculation.Common.Dto;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Micromed.ExternalCalculation.HFOExternalCalculation
{
    public class HFOPlugin : IExternalCalculationPlugin
    {
        public Guid Guid { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Author { get; private set; }
        public string Version { get; private set; }

        private bool isRunning = false;

        private void CallHFOAnnotate(PluginParametersDto pluginParameters)
        {
            try
            {
                string trcPath = pluginParameters.TraceFilePathList[0].Replace("\\", "/");
                string evtPath = pluginParameters.ExchangeEventFilePath.Replace("\\", "/");

                string appPath = Path.Combine(AppContext.BaseDirectory, "Plugins/hfo_engine/HFO_ENGINE.exe").Replace("\\", "/");
                string args = $"--trc=\"{trcPath}\" --xml=\"{evtPath}\"";

                using (Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = args,
                    WorkingDirectory = Path.GetDirectoryName(appPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }))
                {
                    process.WaitForExit();
                }

                if (File.Exists(evtPath))
                {
                    Debug.WriteLine($"EVT file successfully saved in: {evtPath}");
                    DateTime evtModifiedTime = File.GetLastWriteTimeUtc(evtPath);
                    File.SetLastWriteTimeUtc(trcPath, evtModifiedTime);
                    File.SetLastWriteTimeUtc(evtPath, DateTime.UtcNow);
                    Thread.Sleep(2000);
                }
                else
                {
                    Debug.WriteLine("EVT file was NOT created. Check HFO Engine logs.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to start HFO Engine: " + ex.Message);
            }
        }

        public HFOPlugin()
        {
            Guid = Guid.Parse("1BC8E16D-3C09-40BE-8EC2-F9D7E4F0111C");
            Name = "HFO Engine Calculation";
            Description = "HFO Engine External Calculation plugin";
            Author = "Micromed";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;
        }

        public int Start(PluginParametersDto pluginParameters)
        {
            if (isRunning)
                return 1;

            isRunning = true;
            CallHFOAnnotate(pluginParameters);
            OnProgress(100);
            return 0;
        }

        public bool Stop()
        {
            if (!isRunning)
                return false;

            isRunning = false;
            OnCancelled();
            return true;
        }

        public event EventHandler Completed;
        protected void OnCompleted() => Completed?.Invoke(this, null);

        public event EventHandler Cancelled;
        protected void OnCancelled() => Cancelled?.Invoke(this, null);

        public event EventHandler<string> Error;
        protected void OnError(string e) => Error?.Invoke(this, e);

        public event EventHandler<int> Progress;
        protected void OnProgress(int e) => Progress?.Invoke(this, e);

        public bool NeedTraceFilePathList => true;
        public bool NeedExchangeTraceFilePathList => false;
        public bool NeedExchangeEventFilePath => true;
        public bool NeedExchangeReportFilePath => false;
        public bool NeedExchangeTrendFilePathList => false;
        public bool NeedFilteredData => false;
        public bool DerivationOptionEnabled => false;
        public bool TraceSelectionOptionEnabled => false;
    }
}
