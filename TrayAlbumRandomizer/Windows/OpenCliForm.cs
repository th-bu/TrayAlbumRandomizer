namespace TrayAlbumRandomizer.Windows
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public partial class OpenCliForm : Form
    {
        public OpenCliForm(string windowTitle)
        {
            this.InitializeComponent();

            this.Text = windowTitle;
        }

        /// <see>
        /// https://stackoverflow.com/a/2778971/869120
        /// </see>
        public void OpenProcess(string processFileName, params string[] processArgs)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(processFileName, string.Join(" ", processArgs))
            {
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process process = new Process
            {
                StartInfo = processStartInfo
            };

            if (process.Start())
            {
                process.EnableRaisingEvents = true;

                process.OutputDataReceived += this.ProcessOutputDataReceived;
                process.ErrorDataReceived += this.ProcessErrorDataReceived;
                process.Exited += this.ProcessExited;

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            else
            {
                process = null;
            }
        }

        private void ProcessExited(object sender, EventArgs e)
        {
            Process process = sender as Process;

            process.OutputDataReceived -= this.ProcessOutputDataReceived;
            process.ErrorDataReceived -= this.ProcessErrorDataReceived;
            process.Exited -= this.ProcessExited;

            this.BeginInvoke((Action)(() => this.Activate()));
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e?.Data))
            {
                this.outputBox.BeginInvoke((Action)(() => this.WriteLine("Error: " + e.Data)));
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e?.Data))
            {
                this.outputBox.BeginInvoke((Action)(() => this.WriteLine(e.Data)));
            }
        }

        private void WriteLine(string text)
        {
            this.outputBox.AppendText(text + System.Environment.NewLine);
            this.outputBox.ScrollToCaret();
        }
    }
}
