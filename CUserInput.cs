using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wf_config
{
    internal class CUserInput
    {
        public static CUserInput Instance()
        {
            if (_cui == null)
                _cui = new();
            return _cui;
        }

        public void ShowInfo()
        {
            //Present device config
            if (_devConfig != null)
            {
                Console.WriteLine($"HOST NAME: {_devConfig.HostName}");
                Console.WriteLine($"USE DHCP : {_devConfig.HasDhcp}");
                if (!_devConfig.HasDhcp)
                {
                    Console.WriteLine($"  IP     : {_devConfig.Ip}");
                    Console.WriteLine($"  MASK   : {_devConfig.SubNet}");
                    Console.WriteLine($"  GATEWAY: {_devConfig.Gateway}");
                }
                Console.WriteLine($"BAUDRATE : {_devConfig.BaudRate}");
                Console.WriteLine($"WIFI SSID: {_devConfig.Ssid}");
                Console.WriteLine($"PASSWORD : ******"); //Password's not shown
                Console.WriteLine($"TCP PORT : {_devConfig.Port}");
            }
        }

        public bool RequestConfig()
        {
            Console.Write("Configure (y/n)?:");

            var cki = Console.ReadKey();
            bool r = cki.KeyChar == 'Y' || cki.KeyChar == 'y';

            Console.WriteLine("");
            return r;
        }

        public string GetPassword()
        {
            var pass = string.Empty;
            ConsoleKey key;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    Console.Write("\b \b");
                    pass = pass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    pass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            return pass;
        }

        public void GatherConfig()
        {
            if (_devConfig == null) return;

            Console.WriteLine("");
            //Proceed with configuration
            Console.Write("SSID     : ");
            _devConfig.Ssid = Console.ReadLine();
            Console.Write("PASSWORD : ");
            _devConfig.Password = GetPassword();
            Console.Write("HOST NAME: ");
            _devConfig.HostName = Console.ReadLine();
            Console.Write("DHCP(y/n): ");
            _devConfig.HasDhcp = Console.ReadLine()?.ToLower() == "y";
            if (!_devConfig.HasDhcp)
            {
                Console.Write("  IP     : ");
                _devConfig.Ip = Console.ReadLine();
                Console.Write("  MASK   : ");
                _devConfig.SubNet = Console.ReadLine();
                Console.Write("  GATEWAY: ");
                _devConfig.Gateway = Console.ReadLine();
            }
            Console.Write("BAUDRATE : ");
            _devConfig.BaudRate = Console.ReadLine();
            Console.Write("TCP PORT : ");
            _devConfig.Port = Console.ReadLine();
        }

        private CUserInput() 
        {
            CConfig? _cfg = CConfig.Instance();

            if (_cfg != null)
                _devConfig = _cfg.ReadFromDevice();
        }

        private static CWiFiCfg? _devConfig;
        private static CUserInput? _cui;
    }
}
