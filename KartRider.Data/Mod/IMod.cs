using System;

namespace KartRider;

public interface IMod
{
    string Name { get; }
    string Version { get; }
    string Description { get; }

    void OnInitialize();
    void OnUninitialize();
}
