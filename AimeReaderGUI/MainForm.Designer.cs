namespace AimeReaderGUI
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.PortCom = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.ComLogInp = new System.Windows.Forms.TextBox();
            this.ActionLogInp = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.StartBtn = new System.Windows.Forms.Button();
            this.SwipeBtn = new System.Windows.Forms.Button();
            this.P1Rad = new System.Windows.Forms.RadioButton();
            this.P2Rad = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.CardNumCom = new System.Windows.Forms.ComboBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // PortCom
            // 
            this.PortCom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PortCom.FormattingEnabled = true;
            this.PortCom.Location = new System.Drawing.Point(74, 25);
            this.PortCom.Name = "PortCom";
            this.PortCom.Size = new System.Drawing.Size(110, 20);
            this.PortCom.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(33, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "串口:";
            // 
            // ComLogInp
            // 
            this.ComLogInp.Location = new System.Drawing.Point(6, 20);
            this.ComLogInp.Multiline = true;
            this.ComLogInp.Name = "ComLogInp";
            this.ComLogInp.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ComLogInp.Size = new System.Drawing.Size(488, 474);
            this.ComLogInp.TabIndex = 2;
            // 
            // ActionLogInp
            // 
            this.ActionLogInp.Location = new System.Drawing.Point(6, 20);
            this.ActionLogInp.Multiline = true;
            this.ActionLogInp.Name = "ActionLogInp";
            this.ActionLogInp.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.ActionLogInp.Size = new System.Drawing.Size(488, 474);
            this.ActionLogInp.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ComLogInp);
            this.groupBox1.Location = new System.Drawing.Point(12, 66);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(500, 500);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "串口日志";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.ActionLogInp);
            this.groupBox2.Location = new System.Drawing.Point(518, 66);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(500, 500);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "操作日志";
            // 
            // StartBtn
            // 
            this.StartBtn.Location = new System.Drawing.Point(190, 25);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(54, 20);
            this.StartBtn.TabIndex = 6;
            this.StartBtn.Text = "开始";
            this.StartBtn.UseVisualStyleBackColor = true;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // SwipeBtn
            // 
            this.SwipeBtn.Location = new System.Drawing.Point(614, 25);
            this.SwipeBtn.Name = "SwipeBtn";
            this.SwipeBtn.Size = new System.Drawing.Size(54, 20);
            this.SwipeBtn.TabIndex = 7;
            this.SwipeBtn.Text = "刷卡";
            this.SwipeBtn.UseVisualStyleBackColor = true;
            this.SwipeBtn.Click += new System.EventHandler(this.SwipeBtn_Click);
            // 
            // P1Rad
            // 
            this.P1Rad.AutoSize = true;
            this.P1Rad.Checked = true;
            this.P1Rad.Location = new System.Drawing.Point(316, 27);
            this.P1Rad.Name = "P1Rad";
            this.P1Rad.Size = new System.Drawing.Size(35, 16);
            this.P1Rad.TabIndex = 9;
            this.P1Rad.TabStop = true;
            this.P1Rad.Text = "1P";
            this.P1Rad.UseVisualStyleBackColor = true;
            // 
            // P2Rad
            // 
            this.P2Rad.AutoSize = true;
            this.P2Rad.Location = new System.Drawing.Point(357, 27);
            this.P2Rad.Name = "P2Rad";
            this.P2Rad.Size = new System.Drawing.Size(35, 16);
            this.P2Rad.TabIndex = 10;
            this.P2Rad.Text = "2P";
            this.P2Rad.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(269, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 12);
            this.label2.TabIndex = 11;
            this.label2.Text = "位置: ";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(412, 28);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 12);
            this.label3.TabIndex = 12;
            this.label3.Text = "卡号:";
            // 
            // CardNumCom
            // 
            this.CardNumCom.FormattingEnabled = true;
            this.CardNumCom.Items.AddRange(new object[] {
            "39454215743860828634",
            "39454193547934046612"});
            this.CardNumCom.Location = new System.Drawing.Point(453, 25);
            this.CardNumCom.Name = "CardNumCom";
            this.CardNumCom.Size = new System.Drawing.Size(155, 20);
            this.CardNumCom.TabIndex = 13;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1029, 574);
            this.Controls.Add(this.CardNumCom);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.P2Rad);
            this.Controls.Add(this.P1Rad);
            this.Controls.Add(this.SwipeBtn);
            this.Controls.Add(this.StartBtn);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PortCom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Aime卡模拟器";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox PortCom;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ComLogInp;
        private System.Windows.Forms.TextBox ActionLogInp;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.Button SwipeBtn;
        private System.Windows.Forms.RadioButton P1Rad;
        private System.Windows.Forms.RadioButton P2Rad;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox CardNumCom;
    }
}

