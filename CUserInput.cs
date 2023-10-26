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
                Console.WriteLine();
                Console.WriteLine("DEVICE INFORMATION COLLECTED");
                Console.WriteLine("----------------------------");
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
                Console.WriteLine();
            }
        }

        private bool MakeQuestion(string question)
        {
            Console.Write(question);

            var cki = Console.ReadKey();
            bool r = cki.KeyChar == 'Y' || cki.KeyChar == 'y';

            Console.WriteLine();
            return r;
        }

        public bool RequestConfig(bool ignore)
        {
            if (ignore) return true;
            return MakeQuestion("Configure (y/n)?: ");
        }

        public bool RequestWrite(bool ignore)
        {
            if (ignore) return true;
            return MakeQuestion("Write device (y/n)?: ");
        }

        public static string GetPassword()
        {
            return ReadLine(48, '*');
        }

        public static string ReadLine(int nChars = 0, char chPrint = '\0', bool isNumber = false, bool isIp = false, 
            bool noSpaces = false)
        {
            var r = string.Empty;
            ConsoleKey key;

            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && r.Length > 0) //Write backspace to console an into result
                {
                    Console.Write("\b \b");
                    r = r[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {   //Validate input
                    if (    
                            (nChars > 0 && r.Length >= nChars)           || //Verify length if enabled
                            (isNumber && !char.IsDigit(keyInfo.KeyChar)) || //If number, then must be digits
                            (isIp && 
                                (
                                    !char.IsDigit(keyInfo.KeyChar)       || //If IP must have digits
                                    (keyInfo.KeyChar != '.')             || //...must have '.'
                                    (keyInfo.KeyChar == '.'              && //...Or if it's '.'
                                        (r.Count(c => c == '.') > 2      || //...must not have more than 3 '.'
                                            (string.IsNullOrEmpty(r)     &&
                                            r.Last() == '.')                //...and must not have 2 consecutive '.' 
                                        )
                                    )                                    ||
                                    r.Length >= 15                          //...must not have more than 15 chars
                                )
                            )                                            ||
                            (noSpaces && char.IsWhiteSpace(keyInfo.KeyChar))
                        )
                        continue;
                    //Print input
                    if (chPrint != 0) Console.Write(chPrint);
                    else Console.Write(keyInfo.KeyChar);
                    r += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);
            Console.WriteLine();
            return r;
        }

        public void GatherConfig()
        {
            if (_devConfig == null) return;

            Console.WriteLine();
            //Proceed with configuration
            Console.Write("SSID     : ");
            _devConfig.Ssid = ReadLine(63);
            Console.Write("PASSWORD : ");
            _devConfig.Password = GetPassword();
            Console.Write("HOST NAME: ");
            _devConfig.HostName = ReadLine(15, noSpaces: true);
            _devConfig.HasDhcp = MakeQuestion("DHCP(y/n): ");
            if (!_devConfig.HasDhcp)
            {
                Console.Write("  IP     : ");
                _devConfig.Ip = ReadLine(isIp: true);
                Console.Write("  MASK   : ");
                _devConfig.SubNet = ReadLine(isIp: true);
                Console.Write("  GATEWAY: ");
                _devConfig.Gateway = ReadLine(isIp: true);
            }
            Console.Write("BAUDRATE : ");
            _devConfig.BaudRate = ReadLine(7, isNumber: true);
            Console.Write("TCP PORT : ");
            _devConfig.Port = ReadLine(5, isNumber: true);
            Console.WriteLine();
        }

        private CUserInput() 
        {
            _cfg = CConfig.Instance();

            bool canChange = false;
            long idx;

            while (_cfg != null)
            {
                try
                {
                    _devConfig = _cfg.ReadFromDevice();
                    canChange = false;
                }
                catch (Exception ex) when(ex.Message.Contains(CWiFiCfg.BlockStart))
                {   //Other exception will terminate application
                    if (canChange) 
                    {   //Error was not fixed due to invalid ESP8266 flash data
                        Console.WriteLine(ex);
                        Environment.Exit(-1);
                        return;
                    }
                    canChange = true; //Enable changing ConfigPos
                }
                if (canChange)
                {
                    idx = _cfg.GetActualConfigPos();
                    if (idx > 0 && idx != _cfg.ConfigPos)
                    {
                        _cfg.ConfigPos = idx;
                        _cfg.Save();
                    }
                }
                else
                    break;
            }
        }

        private static CWiFiCfg? _devConfig;
        private static CConfig? _cfg;

        public static CWiFiCfg? DevConfig => _devConfig;
        public static CConfig? Configuration => _cfg;

        private static CUserInput? _cui;
    }
}
