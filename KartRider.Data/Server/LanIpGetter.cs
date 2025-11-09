using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace KartRider;

public class LanIpGetter
{
    public static string GetLocalLanIp()
    {
        // 获取所有网络接口
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var iface in interfaces)
        {
            // 过滤掉禁用的接口和回环接口
            if (iface.OperationalStatus != OperationalStatus.Up ||
                iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            // 获取接口的IP信息
            IPInterfaceProperties properties = iface.GetIPProperties();

            foreach (var address in properties.UnicastAddresses)
            {
                // 只保留IPv4地址，且不是回环地址
                if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address))
                {
                    return address.Address.ToString();
                }
            }
        }

        // 如果没有找到有效IP，返回回环地址
        return "127.0.0.1";
    }

    public static List<string> GetAllLocalLanIps()
    {
        List<string> ips = new List<string>();
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var iface in interfaces)
        {
            if (iface.OperationalStatus != OperationalStatus.Up ||
                iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            IPInterfaceProperties properties = iface.GetIPProperties();
            foreach (var address in properties.UnicastAddresses)
            {
                if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(address.Address))
                {
                    ips.Add(address.Address.ToString());
                }
            }
        }
        return ips;
    }
}
