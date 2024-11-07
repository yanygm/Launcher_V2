using System.Collections.Generic;
using System.IO;

namespace KartLibrary.IO;

public delegate T DecodeFieldFunc<T>(BinaryReader reader, Dictionary<short, KartObject>? decodedObjectMap, Dictionary<short, object>? decodedFieldMap);