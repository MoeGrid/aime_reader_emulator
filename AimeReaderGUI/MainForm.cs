using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AimeReaderGUI
{
    public partial class MainForm : Form
    {
        private enum NfcCmd
        {
            SG_NFC_CMD_GET_FW_VERSION = 0x30,
            SG_NFC_CMD_GET_HW_VERSION = 0x32,
            SG_NFC_CMD_RADIO_ON = 0x40,
            SG_NFC_CMD_RADIO_OFF = 0x41,
            SG_NFC_CMD_POLL = 0x42,
            SG_NFC_CMD_MIFARE_SELECT_TAG = 0x43,
            SG_NFC_CMD_MIFARE_SET_KEY_BANA = 0x50,
            SG_NFC_CMD_MIFARE_READ_BLOCK = 0x52,
            SG_NFC_CMD_MIFARE_SET_KEY_AIME = 0x54,
            SG_NFC_CMD_MIFARE_AUTHENTICATE = 0x55, /* guess based on time sent */
            SG_NFC_CMD_RESET = 0x62,
            SG_NFC_CMD_FELICA_ENCAP = 0x71,
        }

        private enum FeliCaCmd
        {
            FELICA_CMD_POLL = 0x00,
            FELICA_CMD_GET_SYSTEM_CODE = 0x0c,
            FELICA_CMD_NDA_A4 = 0xa4,
        }

        private class SwipeObj
        {
            public string Card = "";
            public string IDm = "";
            public string PMm = "";
            public int Type = 0; // 0 无卡 1 Aime 2 FeliCa
        };

        private SerialPort Com;
        private Thread RecvThread;
        private Thread PkgHandleThread;
        private Queue<byte> RecvQueue = new Queue<byte>();

        private SwipeObj[] PlayerState = new SwipeObj[] { new SwipeObj(), new SwipeObj() };

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

        private void SwipeAimeBtn_Click(object sender, EventArgs e)
        {
            if (CardNumCom.Text.Length != 20)
            {
                MessageBox.Show("Aime卡号不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SwipeAimeBtn.Enabled = false;

            var p = PlayerState[P1Rad.Checked ? 0 : P2Rad.Checked ? 1 : 3];
            p.Card = CardNumCom.Text;
            p.Type = 1;

            new System.Threading.Timer(new TimerCallback((obj) =>
            {
                Action action = () =>
                {
                    SwipeAimeBtn.Enabled = true;
                    p.Type = 0;
                };
                Invoke(action);
            }), this, 3000, 0);
        }

        private void SwipeFeliCaBtn_Click(object sender, EventArgs e)
        {
            if (IDmInp.Text.Length != 16)
            {
                MessageBox.Show("IDm不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (PMmInp.Text.Length != 16)
            {
                MessageBox.Show("PMm不正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            SwipeFeliCaBtn.Enabled = false;

            var p = PlayerState[P1Rad.Checked ? 0 : P2Rad.Checked ? 1 : 3];
            p.IDm = IDmInp.Text;
            p.PMm = PMmInp.Text;
            p.Type = 2;

            new System.Threading.Timer(new TimerCallback((obj) =>
            {
                Action action = () =>
                {
                    SwipeFeliCaBtn.Enabled = true;
                    p.Type = 0;
                };
                Invoke(action);
            }), this, 3000, 0);
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
                GotPkg(addr, (NfcCmd)comm, data);
            }
        }

        private void GotPkg(byte addr, NfcCmd comm, byte[] data)
        {
            ActionLog(string.Format("地址: {0} 操作: {1} 数据: {2}", addr.ToString("X2"), comm.ToString(), ToHex(data)));

            // 重置
            if (comm == NfcCmd.SG_NFC_CMD_RESET)
            {
                SendPkg(addr, comm, false, null);
            }

            // 获取固件版本
            if (comm == NfcCmd.SG_NFC_CMD_GET_FW_VERSION)
            {
                SendPkg(addr, comm, false, Encoding.ASCII.GetBytes("TN32MSEC003S F/W Ver1.2E"));
            }

            // 获取硬件版本
            if (comm == NfcCmd.SG_NFC_CMD_GET_HW_VERSION)
            {
                SendPkg(addr, comm, false, Encoding.ASCII.GetBytes("TN32MSEC003S H/W Ver3.0J"));
            }

            // 设置Mifare KeyA
            // 57 43 43 46 76 32
            // WCCFv2
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SET_KEY_AIME)
            {
                SendPkg(addr, comm, false, null);
            }

            // 设置Mifare KeyB
            // 60 90 D0 06 32 F5
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SET_KEY_BANA)
            {
                SendPkg(addr, comm, false, null);
            }

            // 广播开启
            // 03
            if (comm == NfcCmd.SG_NFC_CMD_RADIO_ON)
            {
                SendPkg(addr, comm, false, null);
            }

            // 广播关闭
            if (comm == NfcCmd.SG_NFC_CMD_RADIO_OFF)
            {
                SendPkg(addr, comm, false, null);
            }

            // 检测卡
            if (comm == NfcCmd.SG_NFC_CMD_POLL)
            {
                var p = PlayerState[addr];
                if (p.Type == 1)
                {
                    // Aime
                    var tmp = new byte[] {
                        // uint8_t count
                        0x01,
                        // uint8_t type
                        0x10,
                        // uint8_t id_len
                        0x04,
                        // uint32_t uid
                        0x01, 0x02, 0x03, 0x04,
                    };
                    SendPkg(addr, comm, false, tmp);
                    p.Type = 0;
                }
                else if (p.Type == 2)
                {
                    // FeliCa
                    var tmp = new byte[] {
                        // uint8_t count
                        0x01,
                        // uint8_t type
                        0x20,
                        // uint8_t id_len
                        0x0F,
                        // uint64_t IDm
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        // uint64_t PMm;
                        0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                    };
                    SendPkg(addr, comm, false, tmp);
                    p.Type = 0;
                }
                else
                {
                    // 无卡
                    SendPkg(addr, comm, false, new byte[] { 0x00 });
                }
            }

            // 按UID选择MiFare?
            // xx xx xx xx (四字节Mifare UID)
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SELECT_TAG)
            {
                SendPkg(addr, comm, false, null);
            }

            // Mifare卡认证
            // xx xx xx xx 03 (四字节Mifare UID) 
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_AUTHENTICATE)
            {
                SendPkg(addr, comm, false, null);
            }

            // 从Mifare扇区0读取块1和2.
            // 块0包含"供应商信息"和UID.
            // 块1的内容未知, 可能是AiMe数据库信息.
            // 块2的最后10个字节(十六进制)印在卡上("本地唯一ID")
            // (第3块包含加密密钥, 因此不允许读取)
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_READ_BLOCK)
            {
                // Console.WriteLine("读取扇区: " + data[4]);
                var p = PlayerState[addr];
                var block = data[4];
                if (block == 1)
                {
                    SendPkg(addr, comm, false, new byte[] {
                        0x53, 0x42, 0x53, 0x44, 0x00, 0x00, 0x00, 0x00,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x4F, 0x3D, 0x46
                    });
                }
                else if (block == 2)
                {
                    var tmp = new byte[] {
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x11,
                        0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x11
                    };
                    HexToByte(p.Card).CopyTo(tmp, 6);
                    SendPkg(addr, comm, false, tmp);
                }
            }

            // Felica处理
            if (comm == NfcCmd.SG_NFC_CMD_FELICA_ENCAP)
            {
                // === pub req ===
                // 01 02 03 04 05 06 07 08  IDm
                // 06                       Encap payload length
                // 00                       FeliCa cmd
                var idm = data.Take(8).ToArray();
                var len = data.Skip(8).First();
                var cmd = (FeliCaCmd)data.Skip(9).First();

                if (cmd == FeliCaCmd.FELICA_CMD_POLL)
                {
                    // === req ===
                    // FF FF                    System code
                    // 01                       Request code
                    // 0F                       Time slot
                    var systemCode = data.Skip(10).Take(2).ToArray();
                    var requestCode = data.Skip(12).First();
                    var timeSlot = data.Skip(13).First();

                    Console.WriteLine(string.Format(
                        "IDm: {0} Len: {1} Cmd: {2} SystemCode: {3} RequestCode: {4} TimeSlot: {5}",
                        ToHex(idm),
                        len.ToString("X2"),
                        cmd.ToString(),
                        ToHex(systemCode),
                        requestCode.ToString("X2"),
                        timeSlot.ToString("X2")
                    ));

                    // === res ===
                    // 02                       cmd + 1
                    // 01 02 03 04 05 06 07 08  IDm
                    // 08 07 06 05 04 03 02 01  PMm
                    // 88 99                    Request code == 0x01 ? System code
                    if (requestCode == 0x01)
                    {
                        var tmp = new byte[] {
                            0x00,
                            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                            0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                            0x88, 0x99
                        };
                        tmp[0] = (byte)(cmd + 1);
                        SendPkg(addr, comm, false, tmp);
                    }
                }
                else if (cmd == FeliCaCmd.FELICA_CMD_GET_SYSTEM_CODE)
                {
                    // === req ===
                    // 01 02 03 04 05 06 07 08  IDm2
                    var idm2 = data.Skip(10).Take(8).ToArray();
                    Console.WriteLine(string.Format(
                        "IDm: {0} Len: {1} Cmd: {2} IDm2: {3}",
                        ToHex(idm),
                        len.ToString("X2"),
                        cmd.ToString(),
                        ToHex(idm2)
                    ));
                    // === res ===
                    // 02                       cmd + 1
                    // 01 02 03 04 05 06 07 08  IDm
                    // 01                       Number of system codes
                    // 88 99                    System code
                    var tmp = new byte[] {
                        0x00,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x01,
                        0x88, 0x99
                    };
                    tmp[0] = (byte)(cmd + 1);
                    SendPkg(addr, comm, false, tmp);
                }
                else if (cmd == FeliCaCmd.FELICA_CMD_NDA_A4)
                {
                    // === req ===
                    // 01 02 03 04 05 06 07 08  IDm2
                    // 00                       Unknow
                    var idm2 = data.Skip(10).Take(8).ToArray();
                    var unknow = data.Skip(18).First();
                    Console.WriteLine(string.Format(
                        "IDm: {0} Len: {1} Cmd: {2} IDm2: {3} Unknow: {4}",
                        ToHex(idm),
                        len.ToString("X2"),
                        cmd.ToString(),
                        ToHex(idm2),
                        unknow.ToString("X2")
                    ));
                    // === res ===
                    // 02                       cmd + 1
                    // 01 02 03 04 05 06 07 08  IDm
                    // 00                       Unknow
                    var tmp = new byte[] {
                        0x00,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x00
                    };
                    tmp[0] = (byte)(cmd + 1);
                    SendPkg(addr, comm, false, tmp);
                }
            }
        }

        private void SendPkg(byte addr, NfcCmd comm, bool err, byte[] data)
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
            buff[4] = (byte)comm;
            // 状态
            buff[5] = (byte)(err ? 0x01 : 0x00);
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

            ActionLog(string.Format("地址: {0} 操作: {1} 数据: {2}", addr.ToString("X2"), comm.ToString(), ToHex(data)), true);
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
