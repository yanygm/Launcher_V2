using System.Numerics;
using Veldrid;

namespace KartLibrary.Game.Engine.Render.Veldrid;

public class SceneContext
{
    public DeviceBuffer ViewMatrixBuffer { get; private set; }

    public DeviceBuffer ProjectionMatrixBuffer { get; private set; }

    public Camera SceneCamera { get; private set; }

    public FrameTimeSource TimeSource { get; private set; }

    public ResourceLayout SceneResourceLayout { get; private set; }

    public ResourceSet SceneResourceSet { get; private set; }

    public DeviceObjectCache SceneObjectCache { get; private set; }

    public void CreateDeviceObjects(GraphicsDevice graphicsDevice)
    {
        //IL_000c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0020: Unknown result type (might be due to invalid IL or missing references)
        //IL_0040: Unknown result type (might be due to invalid IL or missing references)
        //IL_0045: Unknown result type (might be due to invalid IL or missing references)
        //IL_0053: Unknown result type (might be due to invalid IL or missing references)
        //IL_0058: Unknown result type (might be due to invalid IL or missing references)
        //IL_005d: Unknown result type (might be due to invalid IL or missing references)
        //IL_008c: Unknown result type (might be due to invalid IL or missing references)
        ResourceFactory resourceFactory = graphicsDevice.ResourceFactory;
        ViewMatrixBuffer = resourceFactory.CreateBuffer(new BufferDescription(64u, (BufferUsage)4));
        ProjectionMatrixBuffer = resourceFactory.CreateBuffer(new BufferDescription(64u, (BufferUsage)4));
        SceneResourceLayout = resourceFactory.CreateResourceLayout(new ResourceLayoutDescription((ResourceLayoutElementDescription[])(object)new ResourceLayoutElementDescription[2]
        {
            new ResourceLayoutElementDescription("ViewInfo", (ResourceKind)0, (ShaderStages)1),
            new ResourceLayoutElementDescription("ProjectionInfo", (ResourceKind)0, (ShaderStages)1)
        }));
        SceneResourceSet = resourceFactory.CreateResourceSet(new ResourceSetDescription(SceneResourceLayout, (BindableResource[])(object)new BindableResource[2]
        {
            (BindableResource)ViewMatrixBuffer,
            (BindableResource)ProjectionMatrixBuffer
        }));
    }

    public void DestroyDeviceObjects()
    {
        DeviceBuffer viewMatrixBuffer = ViewMatrixBuffer;
        if (viewMatrixBuffer != null)
        {
            viewMatrixBuffer.Dispose();
        }

        DeviceBuffer projectionMatrixBuffer = ProjectionMatrixBuffer;
        if (projectionMatrixBuffer != null)
        {
            projectionMatrixBuffer.Dispose();
        }
    }

    public void UpdateCameraBuffers(CommandList commandList)
    {
        commandList.UpdateBuffer<Matrix4x4>(ViewMatrixBuffer, 0u, SceneCamera.ViewMatrix);
        commandList.UpdateBuffer<Matrix4x4>(ProjectionMatrixBuffer, 0u, SceneCamera.ProjectionMatrix);
    }
}