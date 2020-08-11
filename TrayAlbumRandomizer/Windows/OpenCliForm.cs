namespace TrayAlbumRandomizer.Windows
{
    using System;
    using System.Diagnostics;
    using System.Windows.Forms;

    public partial class OpenCliForm : Form
    {
        public OpenCliForm(string windowTitle)
        {
            InitializeComponent();

            Text = windowTitle;
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

                process.OutputDataReceived += ProcessOutputDataReceived;
                process.ErrorDataReceived += ProcessErrorDataReceived;
                process.Exited += ProcessExited;

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

            process.OutputDataReceived -= ProcessOutputDataReceived;
            process.ErrorDataReceived -= ProcessErrorDataReceived;
            process.Exited -= ProcessExited;

            BeginInvoke((Action)(() => Activate()));
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e?.Data))
            {
                _outputBox.BeginInvoke((Action)(() => WriteLine("Error: " + e.Data)));
            }
        }

        private void ProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e?.Data))
            {
                _outputBox.BeginInvoke((Action)(() => WriteLine(e.Data)));
            }
        }

        private void WriteLine(string text)
        {
            _outputBox.AppendText(text + System.Environment.NewLine);
            _outputBox.ScrollToCaret();
        }
    }
}
