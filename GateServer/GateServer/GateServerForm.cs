using GateServer.Properties;
using System.Media;

namespace GateServer
{
    public partial class GateServerForm : Form
    {
        StreamWriter? LogFileStream;
        GateServerCore? GateCore;
        Task? GateCoreTask;
        bool IsServerOpen = false;
        int UserCount = 0;
        public GateServerForm()
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(Settings.Default.LogDirectory))
            {
                InitLogDirectory();
            }
            else
            {
                LogFileStream = new StreamWriter(Settings.Default.LogDirectory, true);
            }
            GateCore = new GateServerCore(this);
            LoginServerConnectListBox.Items.Add("���� �ȵ�");
            LoginServerConnectListBox.BackColor = Color.Red;
            GameServerAcceptListBox.Items.Add("���� ��� �ȵ�");
            GameServerAcceptListBox.BackColor = Color.Red;
            ThreadPool.SetMaxThreads(4, 4);
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

        private void ServerStartClick(object sender, EventArgs e)
        {
            if(IsServerOpen)
            {
                MessageBox.Show("�̹� �������Դϴ�.", "������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SystemSounds.Beep.Play();
            if (MessageBox.Show("������ �����Ͻðڽ��ϱ�?","��������",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.No)
            {
                IsServerOpen = false;
            }    
            if(!GateCore!.InitServer())
            {
                MessageBox.Show("���� �ʱ�ȭ ����", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                IsServerOpen = false;
            }
            GateCoreTask = GateCore.Run();
            IsServerOpen = true;
            Task.WhenAll(GateCoreTask);
        }

        public void SetLoginServerConnected()
        {
            if (LoginServerConnectListBox.InvokeRequired)
            {
                LoginServerConnectListBox.Invoke(new Action(() =>
                {
                    LoginServerConnectListBox.Items[0] = "���� ����";
                    LoginServerConnectListBox.BackColor = Color.Blue;
                }
              ));
            }
            else
            {
                LoginServerConnectListBox.Items[0] = "���� ����";
                LoginServerConnectListBox.BackColor = Color.Blue;
            }
        }
        public void SetLoginServerStopConnected()
        {
            if (LoginServerConnectListBox.InvokeRequired)
            {
                LoginServerConnectListBox.Invoke(new Action(() =>
                {
                    LoginServerConnectListBox.Items[0] = "���� �ȵ�";
                    LoginServerConnectListBox.BackColor = Color.Red;
                }
              ));
            }
            else
            {
                LoginServerConnectListBox.Items[0] = "���� �ȵ�";
                LoginServerConnectListBox.BackColor = Color.Red;
            }
        }
        public void SetLoginServerConnecting()
        {
            if (LoginServerConnectListBox.InvokeRequired)
            {
                LoginServerConnectListBox.Invoke(new Action(() =>
                {
                    LoginServerConnectListBox.Items[0] = "���� �õ���";
                    LoginServerConnectListBox.BackColor = Color.Yellow;
                }
              ));
            }
            else
            {
                LoginServerConnectListBox.Items[0] = "���� �õ���";
                LoginServerConnectListBox.BackColor = Color.Yellow;
            }
        }

        public void SetAllServerStopConnect()
        {
            if (LoginServerConnectListBox.InvokeRequired)
            {
                LoginServerConnectListBox.Invoke(new Action(() =>
                {
                    LoginServerConnectListBox.Items[0] = "���� �ȵ�";
                    LoginServerConnectListBox.BackColor = Color.Red;
                }
              ));
            }
            else
            {
                LoginServerConnectListBox.Items[0] = "���� �ȵ�";
                LoginServerConnectListBox.BackColor = Color.Red;
            }
            if (GameServerAcceptListBox.InvokeRequired)
            {
                GameServerAcceptListBox.Invoke(new Action(() =>
                {
                    GameServerAcceptListBox.Items[0] = "���� ��� �ȵ�";
                    GameServerAcceptListBox.BackColor = Color.Red;
                }
              ));
            }
            else
            {
                GameServerAcceptListBox.Items[0] = "���� ��� �ȵ�";
                GameServerAcceptListBox.BackColor = Color.Red;
            }
        }
        public void IncDecUserCount(bool IsIncrease)
        {
            if(IsIncrease)
            {
                UserCount++;
                if(UserCountTextBox.InvokeRequired)
                {
                    UserCountTextBox.Invoke(new Action(() => { UserCountTextBox.Text = UserCount.ToString(); }));
                }
                else
                    UserCountTextBox.Invoke(new Action(() => { UserCountTextBox.Text = UserCount.ToString(); }));
            }
            else
            {
                UserCount--;
                if (UserCountTextBox.InvokeRequired)
                {
                    UserCountTextBox.Invoke(new Action(() => { UserCountTextBox.Text = UserCount.ToString(); }));
                }
                else
                    UserCountTextBox.Invoke(new Action(() => { UserCountTextBox.Text = UserCount.ToString(); }));
            }
        }
    }
}
