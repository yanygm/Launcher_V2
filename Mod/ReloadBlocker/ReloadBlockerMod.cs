using System;
using System.Reflection;
using KartRider;

namespace KartRider;

public class ReloadBlockerMod : IMod
{
    public string Name => "ReloadBlocker";
    public string Version => "1.0.0";
    public string Description => "拦截 Setting_Button_Click，阻止打开设置窗口";

    private MethodBase? _targetMethod;

    public void OnInitialize()
    {
        try
        {
            _targetMethod = typeof(Launcher).GetMethod("Setting_Button_Click",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_targetMethod == null)
            {
                Console.WriteLine("[ReloadBlocker] 未找到 Launcher.Setting_Button_Click 方法");
                return;
            }

            ModPatchService.AddPrefix(_targetMethod, () =>
            {
                Console.WriteLine("[ReloadBlocker] >>> Setting_Button_Click 被拦截，已阻止打开设置窗口");
                return false;
            });

            Console.WriteLine("[ReloadBlocker] 已成功拦截 Launcher.Setting_Button_Click");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ReloadBlocker] 初始化失败: {ex}");
        }
    }

    public void OnUninitialize()
    {
        if (_targetMethod != null)
            ModPatchService.RemovePrefix(_targetMethod);
        Console.WriteLine("[ReloadBlocker] 已卸载");
    }
}