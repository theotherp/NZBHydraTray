using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SystemTrayApp {
    public partial class Window : Form {

        private Process process;
        private string secretAccessKey;

        public Window() {
            InitializeComponent();
            this.Visible = false;
        }

        private void Window_Load(object sender, EventArgs e) {
            this.Hide();
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            string[] args = Environment.GetCommandLineArgs();
            bool isRestart = false;
            foreach (String arg in args) {
                if (arg.Equals("isrestart")) {
                    isRestart = true;
                }
            }
            startNzbHydraProcess(isRestart);
        }

        public static string randomString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void startNzbHydraProcess(bool isRestart) {
            process = new Process();
            process.StartInfo.FileName = "nzbhydra.exe";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.EnvironmentVariables["STARTEDBYTRAYHELPER"] = "true";
            //Used to auth restart and shutdown without username and password
            secretAccessKey = randomString(16);
            process.StartInfo.EnvironmentVariables["SECRETACCESSKEY"] = secretAccessKey;
            if (isRestart) {
                process.StartInfo.Arguments = "--restarted";
                writeLine("NZBHydraTray: Starting new instance of NZBHydra after a restart was requested", Color.White);
            } else {
                writeLine("NZBHydraTray: Starting new instance of NZBHydra", Color.White);
            }
            process.OutputDataReceived += OutputHandler;
            process.ErrorDataReceived += ErrorOutputHandler;
            process.EnableRaisingEvents = true;

            

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.Exited += ProcessExited;
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
            if (outLine.Data != null) {
                if (outLine.Data.Contains("ERROR")) {
                    writeLine(outLine.Data, Color.Red);
                } else { 
                    writeLine(outLine.Data, Color.White);
                } 
            }
        }

        private void writeLine(string line, Color color) {
            if (line != null) {
                richTextBox1.SelectionBackColor = color;
                richTextBox1.AppendText(line + "\n");
                richTextBox1.ScrollToCaret();
            }
        }

        private void ErrorOutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
            writeLine(outLine.Data, Color.Purple);
        }

        private void Window_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.Hide();
            } else {
                if (process != null && !process.HasExited) {
                    process.Kill();
                }
                
            }
        }

        public void showAndBringToFront() {
            this.Visible = true;
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            richTextBox1.SelectionStart = 0;
            richTextBox1.ScrollToCaret();
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
        }

        private void log(string line) {
            using (StreamWriter file = new System.IO.StreamWriter("NZBHydraTray.log", true)) {
                file.WriteLine(line);
            }
        }

        public void ProcessExited(object sender, EventArgs e) {
            if (process.ExitCode == -1) {
                //An update was executed, so we need to restart ourself. NZBHydra will then be restarted automatically
                
                ProcessStartInfo Info = new ProcessStartInfo();
                Info.Arguments = "/C ping 127.0.0.1 -n 2 && \"" + Application.ExecutablePath + "\" isrestart";
                log("NZBHydra updated itself. Restarting tray tool now using command line: " + Info.Arguments);
                Info.WindowStyle = ProcessWindowStyle.Hidden;
                Info.CreateNoWindow = true;
                Info.FileName = "cmd.exe";
                Process.Start(Info);
                Application.Exit();
            } else if (process.ExitCode == -2) {
                //Restart initialized so we start a new process
                log("NZBHydra requested an restart. Starting a new instance of nzbhydra.exe");
                startNzbHydraProcess(true);
            } else if (process.ExitCode == 0) {
                //Apparently it was shut down so we just quit
                log("NZBHydra shut down with exit code 0. Exiting tray tool.");
                Application.Exit();
            } else if (process.ExitCode == 1073807364) {
                //Upon restart or shut down of windows
                log("NZBHydra was closed by windows. Exiting tray tool.");
                Application.Exit();
            
            } else {
                log("NZBHydra shut down with unknown exit code " + process.ExitCode);
                DialogResult result = MessageBox.Show("NZB Hydra exited with unknown error code " + process.ExitCode + ". Do you want to restart? Otherwise I'll just quit.", "NZB Hydra crashed", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes) {
                    startNzbHydraProcess(false);
                } else {
                    Application.Exit();
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e) {

        }

        public void shutdown() {
            if (!process.HasExited) {
                log("NZBHydra hasn't shut down yet. Waiting...");
                process.Exited -= ProcessExited;
                process.OutputDataReceived -= OutputHandler;
                process.ErrorDataReceived -= ErrorOutputHandler;
                shutdownHydra();
                this.Cursor = Cursors.WaitCursor;
                long before = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                long after;
                while (!process.HasExited) {
                    Thread.Sleep(50);
                    after = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    if ((after - before) / 1000 > 5) {
                        log("NZBHydra hasn't shut after 5 seconds. Killing process. Don't cry!");
                        process.Kill();
                        break;
                    }
                }
                this.Cursor = Cursors.Default;
            }
            Application.Exit();
        }

        public void restartHydra() {
            log("Sending restart command to NZBHydra");
            sendCommandToHydra("restart");
        }

        public void shutdownHydra() {
            log("Sending shutdown command to NZBHydra");
            sendCommandToHydra("shutdown");
        }

        private void sendCommandToHydra(string command) {
            string url = getMainUrlFromSettings();
            url += "/internalapi/" + command;
            url += "?secretaccesskey=" + secretAccessKey;
            log("Sending command via URL " + url);
            var request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode != HttpStatusCode.OK) {
                MessageBox.Show("NZB Hydra didn't respond properly to the request.", "NZB Hydra error");
            } 
        }

        public void openWebinterface() {
            string url = getMainUrlFromSettings();
            System.Diagnostics.Process.Start(url);
        }

        private string getMainUrlFromSettings() {
            //We do this every time again in case some of the settings were changed
            string settingsString = File.ReadAllText("settings.cfg");
            dynamic settings = JsonConvert.DeserializeObject(settingsString);
            string baseUrl = settings.main.baseUrl;
            string url = "";
            if (baseUrl != null && baseUrl != "") {
                url = baseUrl;
            } else {
                string port = settings.main.port;
                string host = settings.main.host;
                bool ssl = settings.main.ssl;
                url = ssl ? "https://" : "http://";
                url += host == "0.0.0.0" ? "127.0.0.1" : host;
                url += ":" + port;
            }

            return url;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e) {

        }

        private void buttonRestart_Click(object sender, EventArgs e) {
            restartHydra();
        }

        private void buttonShutdown_Click(object sender, EventArgs e) {
            shutdown();
        }

        private void richTextBox1_VisibleChanged(object sender, EventArgs e) {
            if (richTextBox1.Visible) {
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
            }
        }

        private void Window_VisibleChanged(object sender, EventArgs e) {
        }

        private void buttonShowInterface_Click(object sender, EventArgs e) {
            openWebinterface();
        }
    }
}
