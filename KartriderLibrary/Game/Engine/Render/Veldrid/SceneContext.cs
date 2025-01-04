using KartLibrary.Game.Engine;
using KartLibrary.Game.Engine.Render.Veldrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace KartLibrary.Game.Engine.Render.Veldrid
{
    // Reference to Veldrid/NeoDemo/SceneContext.cs.
    // https://github.com/veldrid/veldrid/blob/master/src/NeoDemo/SceneContext.cs
    public class SceneContext
    {
        public DeviceBuffer ViewMatrixBuffer { get; private set; }
        public DeviceBuffer ProjectionMatrixBuffer { get; private set; }
        public Camera SceneCamera { get; private set; }
        public FrameTimeSource TimeSource { get; private set; }
        public ResourceLayout SceneResourceLayout { get; private set; }
        public ResourceSet SceneResourceSet {  get; private set; }

        public DeviceObjectCache SceneObjectCache { get; private set; }

        public void CreateDeviceObjects(GraphicsDevice graphicsDevice)
        {
            ResourceFactory factory = graphicsDevice.ResourceFactory;
            ViewMatrixBuffer = factory.CreateBuffer(new BufferDescription(64u, BufferUsage.UniformBuffer));
            ProjectionMatrixBuffer = factory.CreateBuffer(new BufferDescription(64u, BufferUsage.UniformBuffer));
            SceneResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ViewInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ProjectionInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                    ));
            SceneResourceSet = factory.CreateResourceSet(new ResourceSetDescription(SceneResourceLayout, ViewMatrixBuffer, ProjectionMatrixBuffer));
        }

        public void DestroyDeviceObjects()
        {
            ViewMatrixBuffer?.Dispose();
            ProjectionMatrixBuffer?.Dispose();
        }

        public void UpdateCameraBuffers(CommandList commandList)
        {
            commandList.UpdateBuffer(ViewMatrixBuffer, 0, SceneCamera.ViewMatrix);
            commandList.UpdateBuffer(ProjectionMatrixBuffer, 0, SceneCamera.ProjectionMatrix);
        }
    }


}
