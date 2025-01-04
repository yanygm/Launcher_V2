using KartLibrary.Game.Engine.Render;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using KartLibrary.Text;
using Veldrid;
using KartLibrary.Game.Engine.Render.Veldrid;
using KartLibrary.Game.Engine.Render.Veldrid;
using System.IO;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement]
    public class ReTriList : Relement
    {
        public override string ClassName => "ReTriList";

        private int _unknownInt_1;
        private VertexData _vertexData;

        // Veldrid device objects
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private DeviceBuffer _modelUniformBuffer;
        private DeviceBuffer _texPropInfoBuffer;
        private DeviceBuffer _alphaPropInfoBuffer;

        private ResourceSet _localResourceSet;

        private Pipeline _pipeline;

        public VertexData Vertex => _vertexData;

        public ReTriList()
        {

        }

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
            string indendStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indendStr}<ReTriListProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 1, "_unknownInt_1", _unknownInt_1);
            stringBuilder.AppendLine($"{indendStr}</ReTriListProperties>");
            stringBuilder.ConstructPropertyString(indentLevel, "TriList", Vertex);
        }

        #region For veldrid render methods
        protected override void createRelementDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {
            base.createRelementDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
            if (Vertex is null || Vertex.Vertices is null || Vertex.Indexes is null)
                throw new Exception();
            ResourceFactory factory = graphicsDevice.ResourceFactory;

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)Vertex.Vertices.Count() * RenderVertex.SizeOfStruct, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription((uint)(Vertex.Indexes.Length * sizeof(short)), BufferUsage.IndexBuffer));

            _modelUniformBuffer = factory.CreateBuffer(new BufferDescription(64u, BufferUsage.UniformBuffer));
            _alphaPropInfoBuffer = factory.CreateBuffer(new BufferDescription(AlphaPropertyInfo.SizeOfStruct, BufferUsage.UniformBuffer));
            _texPropInfoBuffer = factory.CreateBuffer(new BufferDescription(TexPropertyInfo.SizeOfStruct, BufferUsage.UniformBuffer));

            Shader[]? shaders = sceneContext.SceneObjectCache.GetShaders("RelementShader");
            if (shaders is null)
                throw new Exception();

            ResourceLayout localResourceLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("ModelInfo", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                        new ResourceLayoutElementDescription("TexProperty", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("AlphaProperty", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                        new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)
                    ));

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
        #endregion
    }

}
