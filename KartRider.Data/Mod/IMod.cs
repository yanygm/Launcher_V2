using System;

namespace KartRider;

public interface IMod
{
    string Name { get; }
    string Version { get; }
    string Description { get; }
    bool IsDependencyMod => false; // 依赖Mod不会被卸载

    void OnInitialize();
    void OnUninitialize();
}
