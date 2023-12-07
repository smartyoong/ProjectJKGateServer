using GateServer.Properties;

namespace GateServer
{
    public partial class GateServerForm : Form
    {
        StreamWriter? LogFileStream;
        GateServerCore? GateCore;
        public GateServerForm()
        {
            InitializeComponent();
            LogListBox.BackColor = Color.Red;
            if(string.IsNullOrEmpty(Settings.Default.LogDirectory))
            {
                InitLogDirectory();
            }
            else
            {
                LogFileStream = new StreamWriter(Settings.Default.LogDirectory, true);
            }
            GateCore = new GateServerCore(this);
        }

        public void AddLogWithTime(string Context)
        {
            string Temp = string.Format("{0,-25}{1}", DateTime.Now.ToString(), Context);
            if (LogListBox.InvokeRequired)
            {
                LogListBox.Invoke(new Action<string>(AddLogWithTime), Context);
            }
            else
                LogListBox.Items.Add(Temp);
            LogFileStream!.WriteLine(Temp);
            LogFileStream.Flush();
        }

        public void InitLogDirectory()
        {
            string EXEPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string EXEDirectory = Path.GetDirectoryName(EXEPath)!;
            DateTime CurrentTime = DateTime.Now;
            string FormattedTime = CurrentTime.ToString("yyyy-MM-dd-HH-mm-ss");
            string LogDirectory = Path.Combine(EXEDirectory, $"Logs");
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            string LogFilePath = Path.Combine(LogDirectory, $"LOG{FormattedTime}.txt");
            if (!File.Exists(LogFilePath))
            {
                File.Create(LogFilePath).Close();
                LogFileStream = new StreamWriter(LogFilePath, true);
                Settings.Default.LogDirectory = LogDirectory;
            }
        }

    }
}
