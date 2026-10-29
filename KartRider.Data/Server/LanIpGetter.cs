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

    /// <summary>
    /// 检查本机是否拥有公网 IPv6 地址
    /// </summary>
    public static bool CheckHasPublicIpv6()
    {
        // 遍历所有网络接口
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // 排除禁用、非运行中的网卡
            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            // 获取该网卡的 IP 信息
            IPInterfaceProperties ipProps = ni.GetIPProperties();
            foreach (UnicastIPAddressInformation ipInfo in ipProps.UnicastAddresses)
            {
                IPAddress ip = ipInfo.Address;

                // 只处理 IPv6
                if (ip.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;

                // 判断是否为公网 IPv6
                if (IsPublicIpv6Address(ip))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 核心判断：一个 IPv6 地址是否为公网地址
    /// </summary>
    private static bool IsPublicIpv6Address(IPAddress ipv6Address)
    {
        // 1. 排除环回地址 ::1
        if (IPAddress.IsLoopback(ipv6Address))
            return false;

        // 2. 排除本地链路地址（fe80::/10 开头，局域网用）
        if (ipv6Address.IsIPv6LinkLocal)
            return false;

        // 3. 排除唯一本地地址（fc00::/7 开头，内网专用）
        if (ipv6Address.IsIPv6UniqueLocal)
            return false;

        // 4. 排除自动隧道/ISATAP 等特殊地址
        if (ipv6Address.IsIPv6SiteLocal || ipv6Address.IsIPv6Teredo)
            return false;

        // 剩下的就是公网 IPv6 地址
        return true;
    }

    // 是否IPv4
    public static bool IsIPv4(string ip)
    {
        return IPAddress.TryParse(ip, out var addr)
            && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
    }

    // 是否IPv6
    public static bool IsIPv6(string ip)
    {
        return IPAddress.TryParse(ip, out var addr)
            && addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
    }
}
