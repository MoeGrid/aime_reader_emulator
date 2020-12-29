// TestCpp.cpp : 此文件包含 "main" 函数。程序执行将在此处开始并结束。
//

#include "windows.h"
#include <iostream>

/*
len:  00
addr: 06
seq:  50
comm: 06
plen: 60


0xE0
0x0B 0x00 0x06 0x50 0x06 0x60 0x90 0xD0 0xCF 0x06 0x32 0xF5 0x54

*/

byte data[] = { 0xE0, 0x0B, 0x00, 0x06, 0x50, 0x06, 0x60, 0x90, 0xD0, 0xCF, 0x06, 0x32, 0xF5, 0x54 };
byte index = 0;

// 封包缓冲区
byte buff[255] = { 0 };


void println(const char *msg) {
    printf_s(msg);
    printf_s("\n");
}

// 从串口读取单字节数据
byte read(bool escape = false) {
    if (index >= sizeof(data))
        while (true) system("pause");
    byte b = data[index];
    index++;
    if (b == 0xD0)
        return read(true);
    if (escape)
        b++;
    return b;
}

// 从串口读取多字节数据
int read(byte* data, int count)
{
    for (int i = 0; i < count; i++)
        *(data + i) = read();
    return count;
}

// 校验
byte checkSum(byte* data, int count) {
    int sum = 0;
    for (int i = 0; i < count; i++)
        sum += *(data + i);
    return (byte)(sum % 256);
}

// 发送封包
void sendPkg(byte addr, byte comm, byte *data = NULL, int len = 0) {




    printf_s("发送数据了啊\n");
}

// 收到封包
void gotPkg(byte addr, byte comm, byte* data, byte payloadLen)
{
    printf_s("=======\n");
    printf_s("addr: %02X\n", addr);
    printf_s("comm: %02X\n", comm);
    printf_s("plen: %02X\n", payloadLen);
    printf_s("=======\n");

    // 只接受地址1和2的封包
    if (addr != 0x00 && addr != 0x01)
        return;

    // 未知. 重启?
    if (comm == 0x62)
    {
        sendPkg(addr, comm);
    }

    // 获取固件版本
    if (comm == 0x30)
    {
        byte s[] = "TN32MSEC003S F/W Ver1.2E";
        sendPkg(addr, comm, s);
    }

    // 获取硬件版本
    if (comm == 0x32)
    {
        byte s[] = "TN32MSEC003S H/W Ver3.0J";
        sendPkg(addr, comm, s);
    }

    // 设置Mifare KeyA
    // 57 43 43 46 76 32
    // WCCFv2
    if (comm == 0x54)
    {
        sendPkg(addr, comm);
    }

    // 设置Mifare KeyB
    // 60 90 D0 06 32 F5
    if (comm == 0x50)
    {
        sendPkg(addr, comm);
    }

    // 不明
    // 03
    if (comm == 0x40)
    {
        sendPkg(addr, comm);
    }

    // 不明
    if (comm == 0x41)
    {
        sendPkg(addr, comm);
    }

    // 检查Mifare卡是否存在?
    if (comm == 0x42)
    {
        // 无卡
        // SendPkg(addr, comm, new byte[] { 0x00 });
        // 有卡
        byte s[] = { 0x01, 0x10, 0x04, 0x01, 0x02, 0x03, 0x04 };
        sendPkg(addr, comm, { 0x00 });
    }

    // 按UID选择MiFare?
    // xx xx xx xx (四字节Mifare UID)
    if (comm == 0x43)
    {
        sendPkg(addr, comm);
    }

    // 按UID选择MiFare?
    // xx xx xx xx (四字节Mifare UID)
    if (comm == 0x44)
    {
        sendPkg(addr, comm);
    }

    // 不明
    // xx xx xx xx 03 (四字节Mifare UID)
    if (comm == 0x55)
    {
        sendPkg(addr, comm);
    }

    // 可能从Mifare扇区0读取块1和2.
    // 块0包含"供应商信息"和UID.
    // 块1的内容未知, 可能是AiMe数据库信息.
    // 块2的最后10个字节(十六进制)印在卡上("本地唯一ID").
    // (第3块包含加密密钥, 因此不允许读取)
    /*
    if (comm == 0x52)
    {
      Console.WriteLine("读取扇区: " + data[4]);
      sendPkg(addr, comm, new byte[] {
        0x00, 0x10,
        0x01, 0x02, 0x03, 0x04, 0x05,
        0x06, 0x07, 0x08, 0x09, 0x0A
      });
    }
    */
}

int main()
{
    while (1) {
        // 读包头
        byte h = read();
        if (h != 0xE0) {
            println("Packet header error.");
            continue;
        }
        // 读取包长
        byte len = read();
        if (len <= 0) {
            println("Packet length error.");
            continue;
        }
        // 读取包体
        buff[0] = len;
        read(buff + 1, len - 1);
        // 读取校验码
        byte sum = read();
        if (sum != checkSum(buff, len)) {
            println("Check sum error.");
            continue;
        }
        // 帧长度 地址 序列号 操作号 荷载长度 数据
        gotPkg(buff[1], buff[3], buff + 5, buff[4]);
    }

    /*
    byte buff[5] = { 0 };


    read(buff + 1, 3);

    printf_s("%d %d %d %d %d\n", *(buff + 0), buff[1], buff[2], buff[3], buff[4]);

    printf_s("%d \n", checkSum(buff, 5));

    std::cout << "Hello World!\n";
    */
}
