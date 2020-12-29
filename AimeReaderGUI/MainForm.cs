using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AimeReaderGUI
{
    public partial class MainForm : Form
    {
        private SerialPort Com;
        private Thread RecvThread;
        private Thread PkgHandleThread;
        private Queue<byte> RecvQueue = new Queue<byte>();

        private string Card = "";
        private int Player = 0;
        private bool Can = false;


        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            GetComList();
        }

        private void StartBtn_Click(object sender, EventArgs e)
        {
            if (PortCom.SelectedIndex == -1)
                return;

            if (StartBtn.Text == "开始")
            {
                try
                {
                    Com = new SerialPort(PortCom.Text, 38400);
                    Com.Open();
                    RecvThread = new Thread(RecvThreadFunc);
                    RecvThread.Start();
                    PkgHandleThread = new Thread(PkgHandleThreadFunc);
                    PkgHandleThread.Start();
                    StartBtn.Text = "结束";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                RecvThread.Abort();
                PkgHandleThread.Abort();
                Com.Close();
                StartBtn.Text = "开始";
            }
        }

        private void SwipeBtn_Click(object sender, EventArgs e)
        {
            if (CardNumCom.Text.Length != 20)
            {
                MessageBox.Show("卡号不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SwipeBtn.Enabled = false;
            Card = CardNumCom.Text;
            Player = P1Rad.Checked ? 1 : P2Rad.Checked ? 2 : 0;
            Can = true;
            new System.Threading.Timer(new TimerCallback((obj) =>
            {
                Action action = () =>
                {
                    SwipeBtn.Enabled = true;
                    Can = false;
                };
                Invoke(action);
            }), this, 5000, 0);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (RecvThread != null && RecvThread.IsAlive)
                RecvThread.Abort();
            if (PkgHandleThread != null && PkgHandleThread.IsAlive)
                PkgHandleThread.Abort();
            if (Com != null && Com.IsOpen)
                Com.Close();
        }

        private void RecvThreadFunc()
        {
            while (true)
            {
                int len = Com.BytesToRead;
                if (len > 0)
                {
                    var buff = new byte[len];
                    Com.Read(buff, 0, len);
                    foreach (var i in buff)
                        RecvQueue.Enqueue(i);
                    ComLog(buff);
                }
            }
        }

        private void PkgHandleThreadFunc()
        {
            while (true)
            {
                // 读包头
                byte h = Read();
                if (h != 0xE0)
                {
                    ActionLog("包头错误: " + h.ToString("X2"));
                    continue;
                }
                // 读取包长
                byte len = Read();
                if (len <= 0)
                {
                    ActionLog("包长错误: " + len.ToString("X2"));
                    continue;
                }
                // 读取包体
                var body = new byte[len];
                body[0] = len;
                Read(body, 1, len - 1);
                // 读取校验
                byte sum = Read();
                if (sum != CheckSum(body))
                {
                    ActionLog("校验错误: " + sum.ToString("X2"));
                    continue;
                }
                // 地址
                byte addr = body[1];
                // 操作号
                byte comm = body[3];
                // 数据
                byte[] data = body.Skip(5).Take(body[4]).ToArray();
                // 处理封包
                GotPkg(addr, comm, data);
            }
        }

        private void GotPkg(byte addr, byte comm, byte[] data)
        {
            ActionLog(string.Format("地址: {0} 操作: {1} 数据: {2}", addr.ToString("X2"), comm.ToString("X2"), ToHex(data)));

            // 未知. reset?
            if (comm == 0x62)
            {
                SendPkg(addr, comm, null);
            }

            // 获取固件版本
            if (comm == 0x30)
            {
                SendPkg(addr, comm, Encoding.ASCII.GetBytes("TN32MSEC003S F/W Ver1.2E"));
            }

            // 获取硬件版本
            if (comm == 0x32)
            {
                SendPkg(addr, comm, Encoding.ASCII.GetBytes("TN32MSEC003S H/W Ver3.0J"));
            }

            // 设置Mifare KeyA
            // 57 43 43 46 76 32
            // WCCFv2
            if (comm == 0x54)
            {
                SendPkg(addr, comm, null);
            }

            // 设置Mifare KeyB
            // 60 90 D0 06 32 F5
            if (comm == 0x50)
            {
                SendPkg(addr, comm, null);
            }

            // 检查读卡器连接?
            // 03
            if (comm == 0x40)
            {
                SendPkg(addr, comm, null);
            }

            // 检查读卡器连接?
            if (comm == 0x41)
            {
                SendPkg(addr, comm, null);
            }

            // 检卡
            if (comm == 0x42)
            {
                if(Can && addr + 1 == Player)
                {
                    // 有卡
                    SendPkg(addr, comm, new byte[] { 0x01, 0x10, 0x04, 0x70, 0x0A, 0xE2, 0xBF });
                    Can = false;
                }
                else
                {
                    // 无卡
                    SendPkg(addr, comm, new byte[] { 0x00 });
                }
            }

            // 防冲撞?
            // xx xx xx xx (四字节Mifare UID)
            if (comm == 0x43)
            {
                SendPkg(addr, comm, null);
            }

            // 退卡?
            // xx xx xx xx (四字节Mifare UID)
            // if (comm == 0x44)
            // {
            //     SendPkg(addr, comm, null);
            // }

            // 选卡?
            // xx xx xx xx 03 (四字节Mifare UID) 
            if (comm == 0x55)
            {
                SendPkg(addr, comm, null);
            }

            // 读卡
            // 可能从Mifare扇区0读取块1和2.
            // 块0包含"供应商信息"和UID.
            // 块1的内容未知, 可能是AiMe数据库信息.
            // 块2的最后10个字节(十六进制)印在卡上("本地唯一ID").
            // (第3块包含加密密钥, 因此不允许读取)
            if (comm == 0x52)
            {
                // Console.WriteLine("读取扇区: " + data[4]);
                if (data[4] == 1)
                {
                    SendPkg(addr, comm, new byte[] {
                        0x00, 0x10,
                        0x53, 0x42, 0x53, 0x44, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x4F, 0x3D, 0x46
                    });
                }
                else if (data[4] == 2)
                {
                    var tmp = new byte[] {
                        0x00, 0x10,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x11,
                        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11
                    };
                    HexToByte(Card).CopyTo(tmp, 8);
                    SendPkg(addr, comm, tmp);
                }
            }
        }

        private void SendPkg(byte addr, byte comm, byte[] data)
        {
            byte dataLen = data == null ? (byte)0 : (byte)data.Length;
            byte allLen = (byte)(dataLen + 8);
            byte bodyLen = (byte)(dataLen + 6);

            var buff = new byte[allLen];

            // 包头
            buff[0] = 0xE0;
            // 包长
            buff[1] = bodyLen;
            // 地址
            buff[2] = addr;
            // 序号
            buff[3] = 0x00;
            // 操作
            buff[4] = comm;
            // 状态
            buff[5] = 0x00;
            // 荷载长度
            buff[6] = dataLen;
            // 数据
            if (data != null && data.Length > 0)
                Array.Copy(data, 0, buff, 7, dataLen);
            // 校验
            var tmp = buff.Skip(1).Take(bodyLen).ToArray();
            buff[7 + dataLen] = CheckSum(tmp);
            // 发送
            Write(buff);

            ActionLog(string.Format("地址: {0} 操作: {1} 数据: {2}", addr.ToString("X2"), comm.ToString("X2"), ToHex(data)), true);
        }

        private byte Read(bool escape = false)
        {
            while (RecvQueue.Count <= 0)
                Thread.Sleep(10);
            byte ret = RecvQueue.Dequeue();
            if (ret == 0xD0)
                return Read(true);
            if (escape)
                ret++;
            return ret;
        }

        private int Read(byte[] buffer, int offset, int count)
        {
            for (var i = 0; i < count; i++)
                buffer[offset + i] = Read();
            return count;
        }

        private void Write(byte[] data)
        {
            List<byte> escapeData = new List<byte>();
            for (var i = 0; i < data.Length; i++)
            {
                if (i == 0 || (data[i] != 0xE0 && data[i] != 0xD0))
                {
                    escapeData.Add(data[i]);
                }
                else
                {
                    escapeData.Add(0xD0);
                    escapeData.Add((byte)(data[i] - 1));
                }
            }
            byte[] arr = escapeData.ToArray();
            ComLog(arr, true);
            Com.Write(arr, 0, arr.Length);
        }

        private void GetComList()
        {
            RegistryKey keyCom = Registry.LocalMachine.OpenSubKey("Hardware\\DeviceMap\\SerialComm");
            if (keyCom != null)
            {
                string[] sSubKeys = keyCom.GetValueNames();
                PortCom.Items.Clear();
                foreach (string sName in sSubKeys)
                {
                    string sValue = (string)keyCom.GetValue(sName);
                    PortCom.Items.Add(sValue);
                }
            }
        }

        private string ToHex(byte[] data)
        {
            if (data == null)
                return "";
            StringBuilder builder = new StringBuilder();
            foreach (var i in data)
                builder.Append(i.ToString("X2"));
            return builder.ToString();
        }

        private void ComLog(byte[] data, bool isSend = false)
        {
            Action action = () =>
            {
                ComLogInp.AppendText(DateTime.Now.ToString() + (isSend ? " >>> " : " <<< ") + ToHex(data) + "\r\n");
            };
            Invoke(action);
        }


        private void ActionLog(string msg, bool isSend = false)
        {
            Action action = () =>
            {
                ActionLogInp.AppendText(DateTime.Now.ToString() + (isSend ? " >>> " : " <<< ") + msg + "\r\n");
            };
            Invoke(action);
        }

        private byte CheckSum(byte[] data)
        {
            int checksum = 0;
            foreach (var i in data)
                checksum += i;
            return (byte)(checksum % 256);
        }

        private byte[] HexToByte(string str)
        {
            byte[] b = new byte[str.Length / 2];
            for (int i = 0; i < str.Length / 2; i++)
                b[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            return b;
        }
    }
}
