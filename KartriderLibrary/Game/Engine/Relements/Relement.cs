using KartLibrary.Game.Engine.Properities;
using KartLibrary.Game.Engine.Tontrollers;
using KartLibrary.IO;
using KartLibrary.Xml;
using KartLibrary.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Game.Engine.Render.Veldrid;
using Vulkan;
using Veldrid;
using KartLibrary.Game.Engine;
using KartLibrary.Game.Engine.Render.Veldrid;
using System.IO;

namespace KartLibrary.Game.Engine.Relements
{
    [KartObjectImplement]
    public class Relement : NamedObject, IList<Relement>, IRenderable
    {
        public override string ClassName => "Relement";

        #region Members
        private Relement? _parent;

        private List<Relement> _container = new List<Relement>();

        private Matrix4x4 _transform;
        private Vector3 _position;
        private Vector3 _scale;

        private BoundingBox _boundingBox;
        private byte _unknownByte_70;
        private float _unknownFloat_74;
        private BoundingBox _unknownBB_78;
        private float _unknownFloat_90;
        private byte _unknownByte_94;

        private VisTontroller? _visTontroller;
        private PRSTontroller? _prsTontroller;
        private KartObject? _unknownKartObj_a4;

        private AlphaProperty? _alphaProperty;
        private BackFaceProperty? _backfaceProperty;
        private KartObject? _fogProperty; // fogProperty
        private MtlProperty? _mtlProperty; // mtlProperty
        private TexProperty? _texProperty;
        private KartObject? _toonProperty; // toonProperty
        private KartObject? _wireProperty; // wireProperty
        private ZBufProperty? _zbufProperty;

        private BinaryXmlTag? _additionalProp;
        #endregion

        #region Properties
        public Relement? Parent => _parent;

        public Matrix4x4 Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        public Vector3 Scale
        {
            get => _scale;
            set => _scale = value;
        }

        public BoundingBox Bounding
        {
            get => _boundingBox;
            set => _boundingBox = value;
        }

        public int Count => _container.Count;

        public Relement this[int index] { get => _container[index]; set => _container[index] = value; }

        public bool IsReadOnly => false;

        public VisTontroller? VisTontroller
        {
            get => _visTontroller;
            set => _visTontroller = value;
        }

        public PRSTontroller? PRSTontroller
        {
            get => _prsTontroller;
            set => _prsTontroller = value;
        }

        public KartObject? UnknownTontroller
        {
            get => _unknownKartObj_a4;
            set => _unknownKartObj_a4 = value;
        }

        public AlphaProperty? Alpha
        {
            get => _alphaProperty;
            set => _alphaProperty = value;
        }

        public BackFaceProperty? BackFace
        {
            get => _backfaceProperty;
            set => _backfaceProperty = value;
        }

        public KartObject? Fog
        {
            get => _fogProperty;
            set => _fogProperty = value;
        }

        public MtlProperty? MTL
        {
            get => _mtlProperty;
            set => _mtlProperty = value;
        }

        public TexProperty? Tex
        {
            get => _texProperty;
            set => _texProperty = value;
        }

        public KartObject? Toon
        {
            get => _toonProperty;
            set => _toonProperty = value;
        }

        public KartObject? Wire
        {
            get => _wireProperty;
            set => _wireProperty = value;
        }

        public ZBufProperty? ZBuf
        {
            get => _zbufProperty;
            set => _zbufProperty = value;
        }

        public BinaryXmlTag? Additional
        {
            get => _additionalProp;
            set => _additionalProp = value;
        }
        #endregion

        #region Constructors
        public Relement()
        {

        }
        #endregion

        #region Relement Methods
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            constructString(stringBuilder, 0);
            return stringBuilder.ToString();
        }

        public void Update(ITimeSource timeSource)
        {
            Update(Matrix4x4.Identity, timeSource);
        }

        public void Update(Matrix4x4 parentModelMatrix, ITimeSource timeSource)
        {
            parentModelMatrix = Matrix4x4.CreateScale(_scale) * _transform * Matrix4x4.CreateTranslation(_position) * parentModelMatrix;
            updateRelement(parentModelMatrix, timeSource);
            foreach (Relement child in this)
                child.Update(parentModelMatrix, timeSource);
        }

        private void constructString(StringBuilder stringBuilder, int indentLevel)
        {
            string indendStr = "".PadLeft(indentLevel << 2, ' ');
            stringBuilder.AppendLine($"{indendStr}<{ClassName} name=\"{Name}\">");

            // Print Relement properties
            stringBuilder.AppendLine($"{indendStr}    <RelementProperties>");
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Transform", _transform);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Position", _position);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Scale", _scale);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "BoundingBox", _boundingBox);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownByte_70", _unknownByte_70);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownFloat_74", _unknownFloat_74);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownBB_78", _unknownBB_78);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownFloat_90", _unknownFloat_90);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "_unknownByte_94", _unknownByte_94);

            stringBuilder.ConstructPropertyString(indentLevel + 2, "PRSTontroller", PRSTontroller);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "VisTontroller", VisTontroller);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "UnknownTontroller", UnknownTontroller);

            stringBuilder.ConstructPropertyString(indentLevel + 2, "Alpha", Alpha);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Fog", Fog);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Mtl", MTL);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Tex", Tex);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Toon", Toon);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Wire", Wire);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "ZBuf", ZBuf);
            stringBuilder.ConstructPropertyString(indentLevel + 2, "Additional", Additional);
            stringBuilder.AppendLine($"{indendStr}    </RelementProperties>");

            // Print other info
            constructOtherInfo(stringBuilder, indentLevel);

            // Print children
            stringBuilder.AppendLine($"{indendStr}    <Children>");
            foreach (Relement child in this)
                child.constructString(stringBuilder, indentLevel + 2);
            stringBuilder.AppendLine($"{indendStr}    </Children>");

            stringBuilder.AppendLine($"{indendStr}</{ClassName}>");
        }

        protected virtual void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
        {

        }

        protected virtual void updateRelement(Matrix4x4 parentModelMatrix, ITimeSource timeSource)
        {

        }

        // For Veldrid render.
        protected virtual void createRelementDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {

        }

        protected virtual void updateRelementPerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {

        }

        protected virtual void renderRelement(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {

        }

        protected virtual void destroyRelementObjects()
        {

        }
        #endregion

        #region Implements of KartObject abstract methods
        public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
            int elementCount = reader.ReadInt32();
            for (int i = 0; i < elementCount; i++)
            {
                Relement? child = reader.ReadKartObject<Relement>(decodedObjectMap, decodedFieldMap);
                if (child != null)
                    Add(child);
            }

            _transform = new Matrix4x4();
            _transform[3, 3] = 1;
            for (int i = 0; i < 3; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                _transform[0, i] = x;
                _transform[1, i] = y;
                _transform[2, i] = z;
            }

            _position = reader.ReadVector3();
            _scale = reader.ReadVector3();

            _boundingBox = reader.ReadBoundBox();
            _unknownByte_70 = reader.ReadByte();
            _unknownFloat_74 = reader.ReadSingle();
            _unknownBB_78 = reader.ReadBoundBox();
            _unknownFloat_90 = reader.ReadSingle();
            _unknownByte_94 = reader.ReadByte();

            if (reader.ReadByte() == 1)
                _visTontroller = reader.ReadKartObject<VisTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _prsTontroller = reader.ReadKartObject<PRSTontroller>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _unknownKartObj_a4 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _alphaProperty = reader.ReadKartObject<AlphaProperty>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _backfaceProperty = reader.ReadKartObject<BackFaceProperty>(decodedObjectMap, decodedFieldMap);// backface property
            if (reader.ReadByte() == 1)
                _fogProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _mtlProperty = reader.ReadKartObject<MtlProperty>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _texProperty = reader.ReadKartObject<TexProperty>(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _toonProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _wireProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
            if (reader.ReadByte() == 1)
                _zbufProperty = reader.ReadKartObject<ZBufProperty>(decodedObjectMap, decodedFieldMap);

            if (reader.ReadByte() == 1)
            {
                _additionalProp = reader.ReadField(decodedObjectMap, decodedFieldMap, (reader, decObjMap, decFieldMap) =>
                {
                    return reader.ReadBinaryXmlTag(Encoding.Unicode);
                });
            }
        }

        public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
        {
            base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
        }
        #endregion

        #region Implelements of IList 
        public int IndexOf(Relement item)
        {
            return _container.IndexOf(item);
        }

        public void Insert(int index, Relement item)
        {
            item._parent = this;
            _container.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (_container.Count <= index)
                throw new IndexOutOfRangeException();
            _container[index]._parent = null;
            _container.RemoveAt(index);
        }

        public void Add(Relement item)
        {
            item._parent = this;
            _container.Add(item);
        }

        public void Clear()
        {
            foreach (Relement item in _container)
                item._parent = null;
            _container.Clear();
        }

        public bool Contains(Relement item)
        {
            return _container.Contains(item);
        }

        public void CopyTo(Relement[] array, int arrayIndex)
        {
            _container.CopyTo(array, arrayIndex);
        }

        public bool Remove(Relement item)
        {
            bool result = _container.Remove(item);
            if (result)
                item._parent = null;
            return result;
        }

        public IEnumerator<Relement> GetEnumerator()
        {
            return _container.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Implements of IRenderable
        public void CreateDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {
            createRelementDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
            foreach (Relement child in this)
                child.CreateDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }

        public void UpdatePerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {
            updateRelementPerFrameResources(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
            foreach (Relement child in this)
                child.UpdatePerFrameResources(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }

        public void Render(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
        {
            renderRelement(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
            foreach (Relement child in this)
                child.Render(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }

        public void DestroyAllDeviceObjects()
        {
            destroyRelementObjects();
            foreach (Relement child in this)
                child.DestroyAllDeviceObjects();
        }

        public void Dispose()
        {
            DestroyAllDeviceObjects();
            foreach (Relement child in this)
                child.Dispose();
        }
        #endregion
    }
}
