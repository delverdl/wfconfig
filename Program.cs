// See https://aka.ms/new-console-template for more information

using CommandLine;
using System.Net;
using wf_config;

Console.ForegroundColor = ConsoleColor.White;

Console.WriteLine("WiFi Gateway configuration, version 1.0.");
Console.WriteLine();

var cmdOptions = Parser.Default.ParseArguments<CmdOptions>(args);

cmdOptions.WithParsed(
    options => {
        Process(options);
    });

//Methods
static void Process(CmdOptions options)
{
    Console.WriteLine("Reading device info...");
    Console.WriteLine();

    CUserInput ui = CUserInput.Instance();
    CWiFiCfg? wifiCfg = CUserInput.DevConfig;
    CConfig? cfg = CUserInput.Configuration;

    ui.ShowInfo();
    if (!options.InfoOnly)
    {
        if (options.YesAll)
        {
            if (wifiCfg != null)
            {
                if (!string.IsNullOrEmpty(options.Baudrate) && int.TryParse(options.Baudrate, out _))
                    wifiCfg.BaudRate = options.Baudrate;
                else
                    wifiCfg.BaudRate = "19200";
                if (!string.IsNullOrEmpty(options.Port) && ushort.TryParse(options.Port, out _))
                    wifiCfg.Port = options.Port;
                else
                    wifiCfg.Port = "32594";
                if (string.IsNullOrEmpty(options.Password))
                {
                    Console.Write("PASSWORD : ");
                    wifiCfg.Password = CUserInput.GetPassword();
                }
                else
                    wifiCfg.Password = options.Password;
                wifiCfg.HasDhcp = string.IsNullOrEmpty(options.IpAddress);
                if (!wifiCfg.HasDhcp)
                {
                    if (IPAddress.TryParse(options.IpAddress, out _))
                        wifiCfg.Ip = options.IpAddress;
                    else
                        throw new ArgumentException("Invalid IP address entered!");
                    wifiCfg.SubNet = string.IsNullOrEmpty(options.Mask) ? "255.255.255.0" :
                        IPAddress.Parse(options.Mask).ToString();
                    wifiCfg.Gateway = string.IsNullOrEmpty(options.Gateway) ? "0.0.0.0" :
                        IPAddress.Parse(options.Gateway).ToString();
                }
                if (!string.IsNullOrEmpty(options.HostName))
                {
                    if (options.HostName.Length > 15 || options.HostName.Any(a => char.IsWhiteSpace(a)))
                        throw new ArgumentException("Invalid hostname provided: it must be 15 chars max and " +
                            "shouldn't contain spaces!");
                    wifiCfg.HostName = options.HostName;
                }
                else
                    wifiCfg.HostName = $"PX{DateTime.Now:yyMMddHHmmss}";
                if (!string.IsNullOrEmpty(options.Ssid))
                    wifiCfg.Ssid = options.Ssid;
                else
                    throw new ArgumentException("Must have an SSID to connect to!");
            }
        }
        else
        {
            if (ui.RequestConfig(false))
            {
                ui.GatherConfig();
                if (!ui.RequestWrite(false))
                    return;
            }
            else
                return;
        }
        cfg?.WriteToDevice();
    }
}

//Classes
internal class CmdOptions
{
    [Option('f', "info-only", Default = false, HelpText = "Show device info and exit. Invalidate other options.",
        SetName = "info")]
    public bool InfoOnly { get; set; }

    [Option('y', "yes-all", Default = false, HelpText = "Always yes to internal questions. You " +
        "must use this for the program to process other command line arguments.", SetName = "config")]
    public bool YesAll { get; set; }

    [Option('s', "ssid", HelpText = "WiFi SSID to connect to. This must not be empty", SetName = "config")]
    public string? Ssid { get; set; }

    [Option('w', "password", HelpText = "WiFi Password. If this is not set it'll be asked later.", SetName = "config")]
    public string? Password { get; set; }

    [Option('n', "host-name", HelpText = "Device host name. If not provided, it'll be PX<ISO-DATE-TIME>, " +
        "where 'ISO-DATE-TIME' will have 'yyMMddHHmmss' format.", SetName = "config")]
    public string? HostName { get; set; }

    [Option('i', "ip", HelpText = "Device IP Address for static configuration. If not provided then DHCP " +
        "will be used.", SetName = "config")]
    public string? IpAddress { get; set; }

    [Option('m', "mask", Default = "255.255.255.0", HelpText = "Device mask for static IP.", SetName = "config")]
    public string? Mask { get; set; }

    [Option('g', "gateway", Default = "0.0.0.0", HelpText = "Device gateway for static IP.", SetName = "config")]
    public string? Gateway { get; set; }

    [Option('b', "baud-rate", Default = "19200", HelpText = "Serial baud rate. Other params are: 8N1 and " +
        "hardware control off.", SetName = "config")]
    public string? Baudrate { get; set; }

    [Option('p', "port", Default = "32594", HelpText = "TCP port for device's created server socket. LAN/WAN " +
        "clients should connect to this port and the manual or DHCP provided IP address.", SetName = "config")]
    public string? Port { get; set; }
}
