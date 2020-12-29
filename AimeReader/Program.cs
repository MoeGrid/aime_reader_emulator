using System;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace AimeReader
{
    class Program
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

        private SerialPort Com;

        private Program()
        {
            //var test = new byte[] { 0x0B, 0x00, 0x06, 0x50, 0x06, 0x60, 0x90, 0xD0, 0xCF, 0x06, 0x35, 0xF5 };
            //Console.WriteLine(CheckSum(test).ToString("X2"));

            var c1 = new SerialPort("COM2", 38400);
            c1.Open();
            var c2 = new SerialPort("COM3", 38400);
            c2.Open();

            // c1 转向 c2
            Action t1 = () =>
            {
                while (true)
                {
                    int len = c1.BytesToRead;
                    if (len > 0)
                    {
                        var buff = new byte[len];
                        c1.Read(buff, 0, len);
                        c2.Write(buff, 0, len);
                        Console.WriteLine("!!! <<< " + ToHex(buff));
                    }
                }
            };
            t1.BeginInvoke(null, null);

            // c2 转向 c1
            Action t2 = () =>
            {
                while (true)
                {
                    int len = c2.BytesToRead;
                    if (len > 0)
                    {
                        var buff = new byte[len];
                        c2.Read(buff, 0, len);
                        c1.Write(buff, 0, len);
                        Console.WriteLine("!!! >>> " + ToHex(buff));
                    }
                }
            };
            t2.BeginInvoke(null, null);

            Com = new SerialPort("COM4", 38400);
            Com.Open();

            while (true)
            {
                byte b;

                // 读包头
                b = Read();
                if (b != 0xE0)
                {
                    Console.WriteLine("包头错误: " + b.ToString("X2"));
                    continue;
                }
                // 读取包长
                b = Read();
                if (b <= 0)
                {
                    Console.WriteLine("包长错误");
                    continue;
                }
                // 读取包体
                var body = new byte[b];
                body[0] = b;
                Read(body, 1, b - 1);
                // 读取校验
                b = Read();
                if (b != CheckSum(body))
                {
                    Console.WriteLine("校验错误");
                    continue;
                }
                // 帧长度
                byte len = body[0];
                // 地址
                byte addr = body[1];
                // 序列号
                byte seq = body[2];
                // 操作号
                byte comm = body[3];
                // 荷载长度
                byte payloadLen = body[4];
                // 数据
                byte[] data = body.Skip(5).Take(payloadLen).ToArray();

                // 处理封包
                GotPkg(len, addr, seq, (NfcCmd)comm, data);
            }
        }

        private void GotPkg(byte len, byte addr, byte seq, NfcCmd comm, byte[] data)
        {
            Console.WriteLine(string.Format("<<< 地址: {0} 序号: {1} 操作: {2} 数据: {3}", addr.ToString("X2"), seq.ToString("X2"), comm.ToString("X2"), ToHex(data)));

            // 重置
            if (comm == NfcCmd.SG_NFC_CMD_RESET)
            {
                SendPkg(addr, comm, null);
            }

            // 获取固件版本
            if (comm == NfcCmd.SG_NFC_CMD_GET_FW_VERSION)
            {
                SendPkg(addr, comm, Encoding.ASCII.GetBytes("TN32MSEC003S F/W Ver1.2E"));
            }

            // 获取硬件版本
            if (comm == NfcCmd.SG_NFC_CMD_GET_HW_VERSION)
            {
                SendPkg(addr, comm, Encoding.ASCII.GetBytes("TN32MSEC003S H/W Ver3.0J"));
            }

            // 设置Aime卡密钥
            // 574343467632  
            // WCCFv2
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SET_KEY_AIME)
            {
                SendPkg(addr, comm, null);
            }

            // 设置Bana卡密钥
            // 6090D00632F5
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SET_KEY_BANA)
            {
                SendPkg(addr, comm, null);
            }

            // 广播开启
            // 03
            if (comm == NfcCmd.SG_NFC_CMD_RADIO_ON)
            {
                SendPkg(addr, comm, null);
            }

            // 广播关闭
            if (comm == NfcCmd.SG_NFC_CMD_RADIO_OFF)
            {
                SendPkg(addr, comm, null);
            }

            // 检测卡
            // uint8_t count;
            // MiFare
            // uint8_t type = 0x10;
            // uint8_t id_len;
            // uint32_t uid;
            // Felica
            // uint8_t type = 0x20;
            // uint8_t id_len;
            // uint64_t IDm;
            // uint64_t PMm;
            if (comm == NfcCmd.SG_NFC_CMD_POLL)
            {
                // 无卡
                // SendPkg(addr, comm, new byte[] { 0x00 });
                // 有卡
                SendPkg(addr, comm, new byte[] { 0x01, 0x10, 0x04, 0x70, 0x0A, 0xE2, 0xBF });
            }

            // 按UID选择MiFare?
            // xx xx xx xx (四字节Mifare UID)
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_SELECT_TAG)
            {
                SendPkg(addr, comm, null);
            }

            // 按UID选择MiFare?
            // xx xx xx xx (四字节Mifare UID)
            // if (comm == 0x44)
            // {
            //     SendPkg(addr, comm, null);
            // }

            // Mifare卡认证
            // xx xx xx xx 03 (四字节Mifare UID) 
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_AUTHENTICATE)
            {
                SendPkg(addr, comm, null);
            }

            // 从Mifare扇区0读取块1和2.
            // 块0包含"供应商信息"和UID.
            // 块1的内容未知, 可能是AiMe数据库信息.
            // 块2的最后10个字节(十六进制)印在卡上("本地唯一ID")
            // (第3块包含加密密钥, 因此不允许读取)
            if (comm == NfcCmd.SG_NFC_CMD_MIFARE_READ_BLOCK)
            {
                Console.WriteLine("读取扇区: " + data[4]);
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
                    SendPkg(addr, comm, new byte[] {
                        0x00, 0x10,
                        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x39, 0x45,
                        0x42, 0x15, 0x74, 0x38 , 0x60, 0x82, 0x86, 0x34
                    });
                }
            }

            if (comm == NfcCmd.SG_NFC_CMD_FELICA_ENCAP)
            {

            }
        }

        private void SendPkg(byte addr, NfcCmd comm, byte[] data)
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
            // Com.Write(buff, 0, buff.Length);
            Write(buff);
            Console.WriteLine(string.Format(">>> 地址: {0} 操作: {1} 数据: {2}", addr.ToString("X2"), comm.ToString("X2"), ToHex(data)));
        }

        private byte CheckSum(byte[] data)
        {
            int checksum = 0;
            foreach (var i in data)
                checksum += i;
            return (byte)(checksum % 256);
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


        private void Write(byte[] data)
        {
            for (var i = 0; i < data.Length; i++)
            {
                if (i == 0 || (data[i] != 0xE0 && data[i] != 0xD0))
                {
                    Com.Write(new byte[] { data[i] }, 0, 1);
                }
                else
                {
                    Com.Write(new byte[] { 0xD0 }, 0, 1);
                    Com.Write(new byte[] { (byte)(data[i] - 1) }, 0, 1);
                }
            }
        }

        private byte Read(bool escape = false)
        {
            byte ret = (byte)Com.ReadByte();
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

        static void Main(string[] args)
        {
            new Program();
            Console.ReadKey();
        }
    }
}
