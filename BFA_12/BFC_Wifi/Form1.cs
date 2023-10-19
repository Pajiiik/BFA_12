using System;       //C:\Users\stulhoferp.04\Source\Repos\BFA_12
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace BFC_Wifi
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        static char[] Match =            {'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f','g','h','i','j' ,'k','l','m','n','o','p',
                        'q','r','s','t','u','v','w','x','y','z','A','B','C','D','E','F','G','H','I','J','C','L','M','N','O','P',
                        'Q','R','S','T','U','V','X','Y','Z','!','?',' ','*','-','+'};
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return pingable;
        }


        static async Task<string> ScanWifiNetworksAsync()
        {
            string output = "";
            for (int i = 0; i < 10; i++)
            { 
                try
                {    
                    // Spustit nástroj netsh pro zjištění dostupných Wi-Fi sítí
                    Process process = new Process();
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = "wlan show networks mode=Bssid";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    // Asynchronní čekání na ukončení procesu
                    Task.Run(() => process.WaitForExit());
                    // Přečíst výstup nástroje netsh
                    output = output + process.StandardOutput.ReadToEnd();
                    // Výstup obsahuje informace o dostupných Wi-Fi sítích
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message,"Error");
                }
            }
            return output;
        }

        static HashSet<string> GetUniqueSsids(string input)
        {
            HashSet<string> uniqueSsids = new HashSet<string>();
            string[] lines = input.Split('\n');

            foreach (string line in lines)
            {
                if (line.StartsWith("SSID"))
                {
                    // Použijte regulární výraz k extrakci názvu SSID
                    Match match = Regex.Match(line, @"SSID (\d+) : (.+)");
                    if (match.Success)
                    {
                        string ssid = match.Groups[2].Value.Trim();
                        uniqueSsids.Add(ssid);
                    }
                }
            }

            return uniqueSsids;
        }



        string input;

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Add("WI-FI:");
            Task<string> wifiScan = ScanWifiNetworksAsync();
            input = wifiScan.Result;
            HashSet<string> uniqueSsids = GetUniqueSsids(input);

            foreach (string ssid in uniqueSsids)
            {
                listBox1.Items.Add(ssid);
                UpdateListBoxWidth();
            }

            if (PingHost("8.8.8.8"))
            {

            }
            else
            {

            }
            RemoveEmptyLinesFromListBox(listBox1);
            Update.Start();
        }
        string selectedSsid = "";
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedSsid = listBox1.SelectedItem.ToString();
        }
        public static string GetWifiInfo(string ssid)
        {
            try
            {
                if (ssid != "WI-FI:")
                {
                    // Spustíme příkaz "netsh wlan show all" v konzoli Windows
                    Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WorkingDirectory = Environment.SystemDirectory
                        }
                    };
                    process.Start();

                    // Odešleme příkaz "netsh wlan show all" do konzole
                    process.StandardInput.WriteLine("netsh wlan show all");

                    // Počkáme na dokončení výstupu a získáme ho
                    process.StandardInput.WriteLine("exit");
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    process.Close();
                    string ssidToFind = ssid; // Zde zadejte hledané jméno Wi-Fi sítě
                    string[] lines = output.Split('\n');

                    bool found = false;
                    List<string> wifiInfo = new List<string>();

                    foreach (string line in lines)
                    {
                        if (line.Contains(ssidToFind))
                        {
                            found = true;
                            wifiInfo.Add(line); // Přidejte řádek se začátkem informací o síti
                        }
                        else if (found)
                        {
                            if (string.IsNullOrWhiteSpace(line)) // Konec informací o síti
                            {
                                break;
                            }
                            wifiInfo.Add(line); // Přidejte další řádek informací
                        }
                    }

                    if (found)
                    {
                        string wifiInfoText = string.Join("\n", wifiInfo);
                        MessageBox.Show(wifiInfoText, "Informace o Wi-Fi síti");
                    }
                    else
                    {
                        MessageBox.Show($"Wi-Fi síť s názvem \"{ssidToFind}\" nebyla nalezena.");
                    }

                    return output;
                }
                else
                {
                    RunAllNetshCommandsForWiFi();
                }
                return null;
            }
            catch (Exception ex)
            {
                return "Chyba při získávání informací o Wi-Fi: " + ex.Message;
            }
        }

        static void RunAllNetshCommandsForWiFi()
        {
            // Vytvořte pole s příkazy netsh pro získání informací o Wi-Fi
            string[] netshCommands = {
            "wlan show interfaces",
            "wlan show drivers",
            "wlan show profiles",
            "wlan show hostednetwork"
        };

            foreach (var command in netshCommands)
            {
                ExecuteNetshCommand(command);
            }
        }

        static void ExecuteNetshCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            MessageBox.Show(output);
            process.WaitForExit();
        }

        private void listBox1_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = 30;
        }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0)
            {
                e.DrawBackground();
                e.Graphics.DrawString(listBox1.Items[e.Index].ToString(), e.Font, Brushes.Black, e.Bounds, StringFormat.GenericDefault);
                e.DrawFocusRectangle();
            }
        }
        private void UpdateListBoxWidth()
        {
            // Nastavte šířku ListBoxu na základě počtu položek
            int maxWidth = 0;
            foreach (var item in listBox1.Items)
            {
                int itemWidth = TextRenderer.MeasureText(item.ToString(), listBox1.Font).Width;
                maxWidth = Math.Max(maxWidth, itemWidth);
            }

            listBox1.Width = maxWidth + SystemInformation.VerticalScrollBarWidth;
        }


        private void button1_Click(object sender, EventArgs e)
        {

        }
        public void RemoveEmptyLinesFromListBox(System.Windows.Forms.ListBox listBox)
        {
            for (int i = listBox.Items.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(listBox.Items[i].ToString()))
                {
                    listBox.Items.RemoveAt(i);
                }
            }
        }

        private void Update_Tick(object sender, EventArgs e)
        {
            Task<string> wifiScan = ScanWifiNetworksAsync();
            string input = wifiScan.Result;
            HashSet<string> uniqueSsids = GetUniqueSsids(input);
            listBox1.Items.Clear();
            listBox1.Items.Add("WI-FI:");
            foreach (string ssid in uniqueSsids)
            {
                listBox1.Items.Add(ssid);
                UpdateListBoxWidth();
            }
            RemoveEmptyLinesFromListBox(listBox1);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (selectedSsid == "" || selectedSsid == "Wi-Fi")
            {
                RunAllNetshCommandsForWiFi();
            }
            else
            {
                GetWifiInfo(selectedSsid);
            }
        }
        public static void ConnectToWifi(string ssid, string password)
        {
            // Připravíme příkazy pro připojení k Wi-Fi síti
            string profileName = $"profileName=\"{ssid}\"";
            string keyMaterial = $"keyMaterial=\"{password}\"";

            // Vytvoříme profile pro Wi-Fi síť
            RunNetshCommand($"wlan add profile {profileName} {keyMaterial}");

            // Připojíme se k síti
            RunNetshCommand($"wlan connect {ssid}");
        }

        private static void RunNetshCommand(string arguments)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "BFA",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            process.Close();
        }
        static void Recurse(int Lenght, int Position, string BaseString)
        {
            int Count = 0;

            int maxDegreeOfParallelism = Environment.ProcessorCount; // Nastavte počet vláken dle potřeby
            List<string> results = new List<string>();

            Parallel.ForEach(Match, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, (item) =>
            {
                if (Position < Lenght - 1)
                {
                    string newBaseString = BaseString + item;
                    Recurse(Lenght, Position + 1, newBaseString);

                    ConnectToWifi(selectedSsid,newBaseString);
                        results.Add(newBaseString);
                    
                }
            });
        }
    }
}
