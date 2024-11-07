using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KartLibrary.Game.Engine.Render.Veldrid;
using KartLibrary.IO;
using KartLibrary.Text;
using Veldrid;

namespace KartLibrary.Game.Engine.Relements;

[KartObjectImplement]
public class ReTriList : Relement
{
    private int _unknownInt_1;

    private VertexData _vertexData;

    private DeviceBuffer _vertexBuffer;

    private DeviceBuffer _indexBuffer;

    private DeviceBuffer _modelUniformBuffer;

    private DeviceBuffer _texPropInfoBuffer;

    private DeviceBuffer _alphaPropInfoBuffer;

    private ResourceSet _localResourceSet;

    private Pipeline _pipeline;

    public override string ClassName => "ReTriList";

    public VertexData Vertex => _vertexData;

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        _unknownInt_1 = reader.ReadInt32();
        _vertexData = reader.ReadField(decodedObjectMap, decodedFieldMap, VertexData.Deserialize);
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

    protected override void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
    {
        base.constructOtherInfo(stringBuilder, indentLevel);
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(21, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("<ReTriListProperties>");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel + 1, "_unknownInt_1", _unknownInt_1);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(22, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("</ReTriListProperties>");
        stringBuilder4.AppendLine(ref handler);
        stringBuilder.ConstructPropertyString(indentLevel, "TriList", Vertex);
    }

    protected override void createRelementDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        //IL_0050: Unknown result type (might be due to invalid IL or missing references)
        //IL_0071: Unknown result type (might be due to invalid IL or missing references)
        //IL_0085: Unknown result type (might be due to invalid IL or missing references)
        //IL_0099: Unknown result type (might be due to invalid IL or missing references)
        //IL_00ad: Unknown result type (might be due to invalid IL or missing references)
        //IL_00e4: Unknown result type (might be due to invalid IL or missing references)
        //IL_00e9: Unknown result type (might be due to invalid IL or missing references)
        //IL_00f8: Unknown result type (might be due to invalid IL or missing references)
        //IL_00fd: Unknown result type (might be due to invalid IL or missing references)
        //IL_010c: Unknown result type (might be due to invalid IL or missing references)
        //IL_0111: Unknown result type (might be due to invalid IL or missing references)
        //IL_0120: Unknown result type (might be due to invalid IL or missing references)
        //IL_0125: Unknown result type (might be due to invalid IL or missing references)
        //IL_0134: Unknown result type (might be due to invalid IL or missing references)
        //IL_0139: Unknown result type (might be due to invalid IL or missing references)
        //IL_013e: Unknown result type (might be due to invalid IL or missing references)
        base.createRelementDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        if (Vertex == null || Vertex.Vertices == null || Vertex.Indexes == null)
        {
            throw new Exception();
        }

        ResourceFactory resourceFactory = graphicsDevice.ResourceFactory;
        _vertexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.Vertices.Count() * 20), (BufferUsage)1));
        _indexBuffer = resourceFactory.CreateBuffer(new BufferDescription((uint)(Vertex.Indexes.Length * 2), (BufferUsage)2));
        _modelUniformBuffer = resourceFactory.CreateBuffer(new BufferDescription(64u, (BufferUsage)4));
        _alphaPropInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription(16u, (BufferUsage)4));
        _texPropInfoBuffer = resourceFactory.CreateBuffer(new BufferDescription(16u, (BufferUsage)4));
        if (sceneContext.SceneObjectCache.GetShaders("RelementShader") == null)
        {
            throw new Exception();
        }

        resourceFactory.CreateResourceLayout(new ResourceLayoutDescription((ResourceLayoutElementDescription[])(object)new ResourceLayoutElementDescription[5]
        {
            new ResourceLayoutElementDescription("ModelInfo", (ResourceKind)0, (ShaderStages)1),
            new ResourceLayoutElementDescription("TexProperty", (ResourceKind)0, (ShaderStages)16),
            new ResourceLayoutElementDescription("AlphaProperty", (ResourceKind)0, (ShaderStages)16),
            new ResourceLayoutElementDescription("SurfaceTexture", (ResourceKind)3, (ShaderStages)16),
            new ResourceLayoutElementDescription("SurfaceSampler", (ResourceKind)5, (ShaderStages)16)
        }));
    }

    protected override void updateRelementPerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        base.updateRelementPerFrameResources(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
    }

    protected override void renderRelement(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        base.renderRelement(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
    }

    protected override void destroyRelementObjects()
    {
        base.destroyRelementObjects();
    }
}