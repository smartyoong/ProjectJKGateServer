namespace GateServer
{
    partial class GateServerForm : Form
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            GameServerListBox = new ListBox();
            label1 = new Label();
            UserCountTextBox = new TextBox();
            ServerCloseButton = new Button();
            ServerStartButton = new Button();
            LogListBox = new ListBox();
            SuspendLayout();
            // 
            // GameServerListBox
            // 
            GameServerListBox.FormattingEnabled = true;
            GameServerListBox.ItemHeight = 15;
            GameServerListBox.Location = new Point(12, 12);
            GameServerListBox.Name = "GameServerListBox";
            GameServerListBox.Size = new Size(628, 319);
            GameServerListBox.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(646, 12);
            label1.Name = "label1";
            label1.Size = new Size(66, 15);
            label1.TabIndex = 1;
            label1.Text = "동접자 수 :";
            // 
            // UserCountTextBox
            // 
            UserCountTextBox.Location = new Point(718, 9);
            UserCountTextBox.Name = "UserCountTextBox";
            UserCountTextBox.ReadOnly = true;
            UserCountTextBox.Size = new Size(70, 23);
            UserCountTextBox.TabIndex = 2;
            UserCountTextBox.TextAlign = HorizontalAlignment.Right;
            // 
            // ServerCloseButton
            // 
            ServerCloseButton.Font = new Font("HY견고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerCloseButton.ForeColor = Color.Red;
            ServerCloseButton.Location = new Point(646, 394);
            ServerCloseButton.Name = "ServerCloseButton";
            ServerCloseButton.Size = new Size(142, 42);
            ServerCloseButton.TabIndex = 3;
            ServerCloseButton.Text = "서버 종료";
            ServerCloseButton.UseVisualStyleBackColor = true;
            // 
            // ServerStartButton
            // 
            ServerStartButton.Font = new Font("HY견고딕", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 129);
            ServerStartButton.ForeColor = Color.Blue;
            ServerStartButton.Location = new Point(646, 346);
            ServerStartButton.Name = "ServerStartButton";
            ServerStartButton.Size = new Size(142, 42);
            ServerStartButton.TabIndex = 4;
            ServerStartButton.Text = "서버 시작";
            ServerStartButton.UseVisualStyleBackColor = true;
            // 
            // LogListBox
            // 
            LogListBox.FormattingEnabled = true;
            LogListBox.ItemHeight = 15;
            LogListBox.Location = new Point(12, 342);
            LogListBox.Name = "LogListBox";
            LogListBox.Size = new Size(628, 94);
            LogListBox.TabIndex = 5;
            // 
            // GateServer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(LogListBox);
            Controls.Add(ServerStartButton);
            Controls.Add(ServerCloseButton);
            Controls.Add(UserCountTextBox);
            Controls.Add(label1);
            Controls.Add(GameServerListBox);
            Name = "GateServer";
            Text = "GateServer";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox GameServerListBox;
        private Label label1;
        private TextBox UserCountTextBox;
        private Button ServerCloseButton;
        private Button ServerStartButton;
        private ListBox LogListBox;
    }
}
