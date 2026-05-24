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

    /// <summary>
    /// 判断指定 IP 是否在本机任一局域网网卡的子网内
    /// </summary>
    /// <param name="targetIp">待判断的 IP 字符串</param>
    /// <returns>在同一子网返回 true，否则 false</returns>
    public static bool IsInLocalSubnet(string targetIp)
    {
        if (!IPAddress.TryParse(targetIp, out IPAddress target))
            return false;

        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var iface in interfaces)
        {
            if (iface.OperationalStatus != OperationalStatus.Up ||
                iface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            foreach (var addr in iface.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork ||
                    IPAddress.IsLoopback(addr.Address))
                    continue;

                IPAddress mask = addr.IPv4Mask;
                if (mask == null) continue;

                byte[] localBytes = addr.Address.GetAddressBytes();
                byte[] maskBytes = mask.GetAddressBytes();
                byte[] targetBytes = target.GetAddressBytes();

                bool sameSubnet = true;
                for (int i = 0; i < 4; i++)
                {
                    if ((localBytes[i] & maskBytes[i]) != (targetBytes[i] & maskBytes[i]))
                    {
                        sameSubnet = false;
                        break;
                    }
                }

                if (sameSubnet)
                    return true;
            }
        }

        return false;
    }
}
