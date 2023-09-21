using base93;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace wf_config
{
    public class CWiFiCfg
    {
        public CWiFiCfg() 
        {
            DefaultInit();
        }

        public CWiFiCfg(byte[] data)
        {
            DefaultInit();
            if (data == null || data.Length < 256) return;
            this.StructureAssign(data);
        }

        public CWiFiCfg(CWiFiCfg? cfg)
        {
            DefaultInit();
            if (cfg != null)
                this.StructureAssign(cfg);
        }

        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[256];

            Encoding.UTF8.GetBytes(_initTag ?? string.Empty).CopyTo(bytes, 0);
            Encoding.UTF8.GetBytes(_ssid ?? string.Empty).CopyTo(bytes, 8);
            Encoding.UTF8.GetBytes(_password ?? string.Empty).CopyTo(bytes, 72);
            bytes[136] = (byte)(HasDhcp ? 1 : 0);
            Encoding.UTF8.GetBytes(_ip ?? string.Empty).CopyTo(bytes, 137);
            Encoding.UTF8.GetBytes(_gateway ?? string.Empty).CopyTo(bytes, 153);
            Encoding.UTF8.GetBytes(_initTag ?? string.Empty).CopyTo(bytes, 0);
            return bytes;
        }

        private string? _initTag;
        public string? InitTag { get => _initTag; }

        private string? _ssid;
        public string? Ssid { get => _ssid; set => SetFromValue(ref _ssid, value, 64); }

        private string? _password;
        public string? Password { 
            get => _password; 
            set {
                CBase93? bn = null;

                if (value != null) bn = value;
                SetFromValue(ref _password, bn?.Encoded, 64);
            }
        }

        public bool HasDhcp { get; set; }

        private string? _ip;
        public string? Ip { get => _ip; set => SetFromValue(ref _ip, value, 16); }

        private string? _gateway;
        public string? Gateway { get => _gateway; set => SetFromValue(ref _gateway, value, 16); }

        private string? _subnet;
        public string? SubNet { get => _subnet; set => SetFromValue(ref _subnet, value, 16); }

        private string? _hostName;
        public string? HostName { get => _hostName; set => SetFromValue(ref _hostName, value, 16); }

        private string? _baudRate;
        public string? BaudRate { get => _baudRate; set => SetFromValue(ref _baudRate, value, 8); }

        private string? _port;
        public string? Port { get => _port; set => SetFromValue(ref _port, value, 6); }

        private string? _padding;
        public string? Padding { get => _padding; }

        private string? _endTag;
        public string? EndTag { get => _endTag; }

        private void DefaultInit()
        {
            _initTag = "DxWiFiS\x00";
            _ssid = 64.Zeroed();
            _password = 64.Zeroed();
            HasDhcp = true;
            _ip = 16.Zeroed();
            _gateway = 16.Zeroed();
            _subnet = 16.Zeroed();
            _hostName = 16.Zeroed();
            _baudRate = 8.Zeroed();
            _port = 6.Zeroed();
            _padding = new string('\xD1', 33);
            _endTag = "DxWiFiE\x00";
        }

        private static void SetFromValue(ref string? str, string? value, int maxLength)
        {
            if (value != null && value?.Length < maxLength)
            {
                str = value;
                while (str.Length < maxLength)
                    str += '\x00';
            }
        }
    }

    public static class Extensions
    {
        public static CWiFiCfg StructureAssign(this CWiFiCfg cfg, CWiFiCfg other)
        {
            cfg.Ssid    = other.Ssid;
            cfg.Password= other.Password;
            cfg.HasDhcp = other.HasDhcp;
            cfg.Ip      = other.Ip;
            cfg.Gateway = other.Gateway;
            cfg.SubNet  = other.SubNet;
            cfg.HostName= other.HostName;
            cfg.BaudRate= other.BaudRate;
            cfg.Port    = other.Port;
            return cfg;
        }

        public static CWiFiCfg StructureAssign(this CWiFiCfg cfg, byte[] data)
        {
            cfg.Ssid = Encoding.UTF8.GetString(data, 8, 64);
            cfg.Password = Encoding.UTF8.GetString(data, 72, 64);
            cfg.HasDhcp = data[136] == 1;
            cfg.Ip = Encoding.UTF8.GetString(data, 137, 16);
            cfg.Gateway = Encoding.UTF8.GetString(data, 153, 16);
            cfg.SubNet = Encoding.UTF8.GetString(data, 169, 16);
            cfg.HostName = Encoding.UTF8.GetString(data, 185, 16);
            cfg.BaudRate = Encoding.UTF8.GetString(data, 201, 8);
            cfg.Port = Encoding.UTF8.GetString(data, 209, 6);
            return cfg;
        }

        public static string Zeroed(this int length)
        {
            return new string('\x00', length);
        }

    }

    internal class CConfig
    {
        public static CConfig? Instance()
        {
            if (_config == null)
                _config = JsonConvert.DeserializeObject<CConfig>("wpcfg.json");
            return _config;
        }

        private CConfig() {  }

        private static CConfig? _config;
        private static CWiFiCfg? _wifiCfg;

        public long? ConfigSector { get; set; }

        public long? ConfigPos { get; set; }

        public string? ConfigPort { get; set; }

        public long? ConfigBaudrate { get; set; }

        private StreamWriter? _streamError;
        private bool _errorsWritten;

        public void RunEspTool(bool showProgress, bool readFlash = true)
        {
            Process process = new();

            process.StartInfo.FileName = "./python/python";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = showProgress;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.Arguments =
                $"./esptool/esptool.py -p {ConfigPort} -b {ConfigBaudrate} " +
                $"read_flash 0x{ConfigSector:X} 0x1000 ctx_data.bin";

            if (OperatingSystem.IsWindows())
            {
                process.StartInfo.FileName = process.StartInfo.FileName.Replace('/', '\\');
                process.StartInfo.Arguments = process.StartInfo.Arguments.Replace('/', '\\');
            }

            process.OutputDataReceived += Process_OutputDataReceived; //Standard output processing
            process.ErrorDataReceived += Process_ErrorDataReceived; //Standard error processing

            //Run application
            if (process.Start())
            {
                if (showProgress)
                    process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidProgramException($"Application exited with code {process.ExitCode}");
            }
            else throw new FileLoadException("Unknown error loading ESPTOOL!");
        }

        private void Process_OutputDataReceived(object sender, 
            DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
                Console.Write(outLine.Data);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            {
                if (!string.IsNullOrEmpty(outLine.Data))
                {
                    if (!_errorsWritten)
                    {
                        if (_streamError == null)
                        {
                            try
                            {
                                _streamError = new("errors.log", true);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("ERROR: Could not open errors log file!");
                                Console.WriteLine(e.Message.ToString());
                            }
                        }
                        _streamError?.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}]");
                        _errorsWritten = true;
                    }
                    //Write log
                    _streamError?.WriteLine(outLine.Data);
                    _streamError?.Flush();
                }
            };
        }

        public CWiFiCfg? ReadFromDevice(bool refresh = false)
        {
            if (refresh || _wifiCfg == null)
            {
                RunEspTool(true);
                using (FileStream fs = new("ctx_data.bin", FileMode.Open))
                {
                    fs.Seek(ConfigPos ?? 0, SeekOrigin.Begin);

                    byte[] b = new byte[256];

                    fs.Read(b, 0, b.Length);
                    if (Encoding.UTF8.GetString(b).StartsWith("DxWiFiS"))
                        _wifiCfg = new(b);

                }
            }
            return _wifiCfg;
        }

    }
}
