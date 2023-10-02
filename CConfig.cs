using base93;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace wf_config
{
    public class CWiFiCfg
    {
        private void StructureAssign(CWiFiCfg other)
        {
            _ssid = other.Ssid;
            _password = other.Password;
            HasDhcp = other.HasDhcp;
            _ip = other.Ip;
            _gateway = other.Gateway;
            _subnet = other.SubNet;
            _hostName = other.HostName;
            _baudRate = other.BaudRate;
            _port = other.Port;
        }

        private void StructureAssign(byte[] data)
        {
            _ssid = Encoding.UTF8.GetString(data, 8, 64);
            _password = Encoding.UTF8.GetString(data, 72, 64);
            HasDhcp = data[136] == 1;
            _ip = Encoding.UTF8.GetString(data, 137, 16);
            _gateway = Encoding.UTF8.GetString(data, 153, 16);
            _subnet = Encoding.UTF8.GetString(data, 169, 16);
            _hostName = Encoding.UTF8.GetString(data, 185, 16);
            _baudRate = Encoding.UTF8.GetString(data, 201, 8);
            _port = Encoding.UTF8.GetString(data, 209, 6);
        }

        public CWiFiCfg() 
        {
            DefaultInit();
        }

        public CWiFiCfg(byte[] data)
        {
            DefaultInit();
            if (data == null || data.Length < 256) return;
            StructureAssign(data);
        }

        public CWiFiCfg(CWiFiCfg? cfg)
        {
            DefaultInit();
            if (cfg != null)
                StructureAssign(cfg);
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
            Encoding.UTF8.GetBytes(_subnet ?? string.Empty).CopyTo(bytes, 169);
            Encoding.UTF8.GetBytes(_hostName ?? string.Empty).CopyTo(bytes, 185);
            Encoding.UTF8.GetBytes(_baudRate ?? string.Empty).CopyTo(bytes, 201);
            Encoding.UTF8.GetBytes(_port ?? string.Empty).CopyTo(bytes, 209);
            Encoding.UTF8.GetBytes(_padding ?? string.Empty).CopyTo(bytes, 215);
            Encoding.UTF8.GetBytes(_endTag ?? string.Empty).CopyTo(bytes, 248);
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
            _padding = new string('\x2e', 33);
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
                _config = JsonConvert.DeserializeObject<CConfig>(File.ReadAllText("wpcfg.json"));
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

        public enum ESPAction
        {
            read_flash,
            write_flash,
            erase_region
        }

        public void RunEspTool(bool showProgress, ESPAction action = ESPAction.read_flash)
        {
            const int sectorCount = 0x1000;
            Process process = new();
            string sAct = action.ToString() + " ";

            if (action == ESPAction.erase_region)
                sAct = "--after no_reset " + sAct;
            process.StartInfo.FileName = "./python/python";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = showProgress;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.Arguments =
                $"./esptool/esptool.py -p {ConfigPort} -b {ConfigBaudrate} " + sAct +
                $"0x{ConfigSector:X}";

            if (action != ESPAction.write_flash) //Read or erase size
                process.StartInfo.Arguments += $" 0x{sectorCount:X}";

            if (action < ESPAction.erase_region) //File to read or write
                process.StartInfo.Arguments += " ctx_data.bin";

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
                    throw new InvalidProgramException($"ESPTOOL application exited with error code {process.ExitCode}");
            }
            else throw new FileLoadException("Unknown error loading ESPTOOL!");
        }

        private void Process_OutputDataReceived(object sender, 
            DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                var sav = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(outLine.Data);
                Console.ForegroundColor = sav;
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                var sav = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.Red;
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
                if (Regex.IsMatch(outLine.Data, @"Exception.?:\s+"))
                    Console.WriteLine(outLine.Data);
                Console.ForegroundColor = sav;
            }
            
        }

        public CWiFiCfg? ReadFromDevice(bool refresh = false)
        {
            if (refresh || _wifiCfg == null)
            {
                if (File.Exists("ctx_data.bin")) File.Delete("ctx_data.bin");
                
                //Connect device through USB to read its flash memory
                RunEspTool(true);

                using (FileStream fs = new("ctx_data.bin", FileMode.Open))
                {
                    fs.Seek(ConfigPos ?? 0, SeekOrigin.Begin);

                    byte[] b = new byte[256];

                    fs.Read(b, 0, b.Length);
                    if (Encoding.UTF8.GetString(b).StartsWith("DxWiFiS"))
                        _wifiCfg = new(b);
                    else
                        throw new InvalidDataException("Data sector read from device doesn't contain the configuration " +
                            "array. Verify that 'ctx_data.bin' contains the string 'DxWiFiS' and its byte offset must be " +
                            "properly configured in 'wpcfg.json' file (ConfigPos value).");
                }
            }
            return _wifiCfg;
        }

        private bool ContentsMatch(string f1, string f2)
        {
            if ((new FileInfo(f1)).Length != (new FileInfo(f2)).Length)
                return false;

            byte[] fs1 = File.ReadAllBytes(f1), fs2 = File.ReadAllBytes(f2);

            for (int i = 0; i < fs1.Length && i < fs2.Length; i++)
                if (fs1[i] != fs2[i]) return false;
            return true;
        }

        public void WriteToDevice()
        {
            byte nBk = 0;
            string bkName = string.Empty;

            Console.WriteLine("Writing config to disk...");

            //Back up data
            do
            {
                if (!string.IsNullOrEmpty(bkName) && ContentsMatch("ctx_data.bin", bkName))
                {
                    File.Delete(bkName); //Asure saving to an existing backup with equal contents
                    break;
                }
                bkName = $"ctx_data.bin.{nBk:X2}";
                nBk++;
            }
            while (File.Exists(bkName));
            File.Copy("ctx_data.bin", bkName);
            using (FileStream fs = new("ctx_data.bin", FileMode.Open))
            {
                fs.Seek(ConfigPos ?? 0, SeekOrigin.Begin);
                fs.Write(_wifiCfg?.ToByteArray());
            }
            
            Thread.Sleep(250); //Wait a little bit

            Console.WriteLine("Erasing ESP device flash config region...");
            Console.WriteLine();
            RunEspTool(true, ESPAction.erase_region);

            Console.WriteLine("Writing ESP flash config...");
            Console.WriteLine();
            RunEspTool(true, ESPAction.write_flash);

            Console.WriteLine();
            Console.WriteLine("Done");
            Console.WriteLine("Connect to serial port to verify device!");
        }

    }
}
