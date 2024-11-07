using System;
using Veldrid;

namespace KartLibrary.Game.Engine.Render.Veldrid;

public interface IRenderable : IDisposable
{
    void CreateDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

    void UpdatePerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

    void Render(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

    void DestroyAllDeviceObjects();
}