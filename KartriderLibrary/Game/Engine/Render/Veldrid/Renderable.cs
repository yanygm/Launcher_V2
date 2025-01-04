using KartLibrary.Game.Engine.Render.Veldrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KartLibrary.Game.Engine.Render.Veldrid
{
    public interface IRenderable: IDisposable
    {
        void CreateDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

        void UpdatePerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

        void Render(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache);

        void DestroyAllDeviceObjects();
    }
}
