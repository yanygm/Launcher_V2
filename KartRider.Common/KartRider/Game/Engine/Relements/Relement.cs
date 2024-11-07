using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using KartLibrary.Game.Engine.Properities;
using KartLibrary.Game.Engine.Render.Veldrid;
using KartLibrary.Game.Engine.Tontrollers;
using KartLibrary.IO;
using KartLibrary.Text;
using KartLibrary.Xml;
using Veldrid;

namespace KartLibrary.Game.Engine.Relements;

[KartObjectImplement]
public class Relement : NamedObject, IList<Relement>, ICollection<Relement>, IEnumerable<Relement>, IEnumerable, IRenderable, IDisposable
{
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

    private KartObject? _fogProperty;

    private MtlProperty? _mtlProperty;

    private TexProperty? _texProperty;

    private KartObject? _toonProperty;

    private KartObject? _wireProperty;

    private ZBufProperty? _zbufProperty;

    private BinaryXmlTag? _additionalProp;

    public override string ClassName => "Relement";

    public Relement? Parent => _parent;

    public Matrix4x4 Transform
    {
        get
        {
            return _transform;
        }
        set
        {
            _transform = value;
        }
    }

    public Vector3 Position
    {
        get
        {
            return _position;
        }
        set
        {
            _position = value;
        }
    }

    public Vector3 Scale
    {
        get
        {
            return _scale;
        }
        set
        {
            _scale = value;
        }
    }

    public BoundingBox Bounding
    {
        get
        {
            return _boundingBox;
        }
        set
        {
            _boundingBox = value;
        }
    }

    public int Count => _container.Count;

    public Relement this[int index]
    {
        get
        {
            return _container[index];
        }
        set
        {
            _container[index] = value;
        }
    }

    public bool IsReadOnly => false;

    public VisTontroller? VisTontroller
    {
        get
        {
            return _visTontroller;
        }
        set
        {
            _visTontroller = value;
        }
    }

    public PRSTontroller? PRSTontroller
    {
        get
        {
            return _prsTontroller;
        }
        set
        {
            _prsTontroller = value;
        }
    }

    public KartObject? UnknownTontroller
    {
        get
        {
            return _unknownKartObj_a4;
        }
        set
        {
            _unknownKartObj_a4 = value;
        }
    }

    public AlphaProperty? Alpha
    {
        get
        {
            return _alphaProperty;
        }
        set
        {
            _alphaProperty = value;
        }
    }

    public BackFaceProperty? BackFace
    {
        get
        {
            return _backfaceProperty;
        }
        set
        {
            _backfaceProperty = value;
        }
    }

    public KartObject? Fog
    {
        get
        {
            return _fogProperty;
        }
        set
        {
            _fogProperty = value;
        }
    }

    public MtlProperty? MTL
    {
        get
        {
            return _mtlProperty;
        }
        set
        {
            _mtlProperty = value;
        }
    }

    public TexProperty? Tex
    {
        get
        {
            return _texProperty;
        }
        set
        {
            _texProperty = value;
        }
    }

    public KartObject? Toon
    {
        get
        {
            return _toonProperty;
        }
        set
        {
            _toonProperty = value;
        }
    }

    public KartObject? Wire
    {
        get
        {
            return _wireProperty;
        }
        set
        {
            _wireProperty = value;
        }
    }

    public ZBufProperty? ZBuf
    {
        get
        {
            return _zbufProperty;
        }
        set
        {
            _zbufProperty = value;
        }
    }

    public BinaryXmlTag? Additional
    {
        get
        {
            return _additionalProp;
        }
        set
        {
            _additionalProp = value;
        }
    }

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
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Update(parentModelMatrix, timeSource);
        }
    }

    private void constructString(StringBuilder stringBuilder, int indentLevel)
    {
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder3 = stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(10, 3, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("<");
        handler.AppendFormatted(ClassName);
        handler.AppendLiteral(" name=\"");
        handler.AppendFormatted(base.Name);
        handler.AppendLiteral("\">");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(24, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    <RelementProperties>");
        stringBuilder4.AppendLine(ref handler);
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
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder5 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    </RelementProperties>");
        stringBuilder5.AppendLine(ref handler);
        constructOtherInfo(stringBuilder, indentLevel);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder6 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    <Children>");
        stringBuilder6.AppendLine(ref handler);
        using (IEnumerator<Relement> enumerator = GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                enumerator.Current.constructString(stringBuilder, indentLevel + 2);
            }
        }

        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder7 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("    </Children>");
        stringBuilder7.AppendLine(ref handler);
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder8 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("</");
        handler.AppendFormatted(ClassName);
        handler.AppendLiteral(">");
        stringBuilder8.AppendLine(ref handler);
    }

    protected virtual void constructOtherInfo(StringBuilder stringBuilder, int indentLevel)
    {
    }

    protected virtual void updateRelement(Matrix4x4 parentModelMatrix, ITimeSource timeSource)
    {
    }

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

    public override void DecodeObject(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.DecodeObject(reader, decodedObjectMap, decodedFieldMap);
        int num = reader.ReadInt32();
        for (int i = 0; i < num; i++)
        {
            Relement relement = reader.ReadKartObject<Relement>(decodedObjectMap, decodedFieldMap);
            if (relement != null)
            {
                Add(relement);
            }
        }

        _transform = default(Matrix4x4);
        _transform[3, 3] = 1f;
        for (int j = 0; j < 3; j++)
        {
            float value = reader.ReadSingle();
            float value2 = reader.ReadSingle();
            float value3 = reader.ReadSingle();
            _transform[0, j] = value;
            _transform[1, j] = value2;
            _transform[2, j] = value3;
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
        {
            _visTontroller = reader.ReadKartObject<VisTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _prsTontroller = reader.ReadKartObject<PRSTontroller>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _unknownKartObj_a4 = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _alphaProperty = reader.ReadKartObject<AlphaProperty>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _backfaceProperty = reader.ReadKartObject<BackFaceProperty>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _fogProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _mtlProperty = reader.ReadKartObject<MtlProperty>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _texProperty = reader.ReadKartObject<TexProperty>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _toonProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _wireProperty = reader.ReadKartObject(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _zbufProperty = reader.ReadKartObject<ZBufProperty>(decodedObjectMap, decodedFieldMap);
        }

        if (reader.ReadByte() == 1)
        {
            _additionalProp = reader.ReadField(decodedObjectMap, decodedFieldMap, (BinaryReader reader, Dictionary<short, KartObject>? decObjMap, Dictionary<short, object>? decFieldMap) => reader.ReadBinaryXmlTag(Encoding.Unicode));
        }
    }

    public override void EncodeObject(BinaryWriter writer, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap)
    {
        base.EncodeObject(writer, decodedObjectMap, decodedFieldMap);
    }

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
        {
            throw new IndexOutOfRangeException();
        }

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
        {
            item._parent = null;
        }

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
        bool num = _container.Remove(item);
        if (num)
        {
            item._parent = null;
        }

        return num;
    }

    public IEnumerator<Relement> GetEnumerator()
    {
        return _container.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void CreateDeviceObjects(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        createRelementDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.CreateDeviceObjects(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }
    }

    public void UpdatePerFrameResources(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        updateRelementPerFrameResources(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.UpdatePerFrameResources(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }
    }

    public void Render(GraphicsDevice graphicsDevice, CommandList commandList, SceneContext sceneContext, DeviceObjectCache localDeviceObjectCache)
    {
        renderRelement(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Render(graphicsDevice, commandList, sceneContext, localDeviceObjectCache);
        }
    }

    public void DestroyAllDeviceObjects()
    {
        destroyRelementObjects();
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.DestroyAllDeviceObjects();
        }
    }

    public void Dispose()
    {
        DestroyAllDeviceObjects();
        using IEnumerator<Relement> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Dispose();
        }
    }
}