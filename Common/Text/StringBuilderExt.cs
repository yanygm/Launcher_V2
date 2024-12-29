using System;
using System.Numerics;
using System.Text;

namespace KartLibrary.Text;

public static class StringBuilderExt
{
    public static void ConstructPropertyString<T>(this StringBuilder stringBuilder, int indentLevel, string propertyName, T propertyValue)
    {
        string value = "".PadLeft(indentLevel << 2, ' ');
        StringBuilder stringBuilder2;
        StringBuilder.AppendInterpolatedStringHandler handler;
        if (propertyValue is Vector2)
        {
            object obj = propertyValue;
            Vector2 vector = (Vector2)((obj is Vector2) ? obj : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder3 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(11, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> (");
            handler.AppendFormatted(vector.X, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector.Y, 8, "0.000");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder3.AppendLine(ref handler);
            return;
        }

        if (propertyValue is Vector3)
        {
            object obj2 = propertyValue;
            Vector3 vector2 = (Vector3)((obj2 is Vector3) ? obj2 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder4 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(13, 6, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> (");
            handler.AppendFormatted(vector2.X, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector2.Y, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector2.Z, 8, "0.000");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder4.AppendLine(ref handler);
            return;
        }

        if (propertyValue is Vector4)
        {
            object obj3 = propertyValue;
            Vector4 vector3 = (Vector4)((obj3 is Vector4) ? obj3 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder5 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(15, 7, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> (");
            handler.AppendFormatted(vector3.X, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector3.Y, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector3.Z, 8, "0.000");
            handler.AppendLiteral(", ");
            handler.AppendFormatted(vector3.W, 8, "0.000");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder5.AppendLine(ref handler);
            return;
        }

        if (propertyValue is Matrix4x4)
        {
            object obj4 = propertyValue;
            Matrix4x4 matrix4x = (Matrix4x4)((obj4 is Matrix4x4) ? obj4 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder6 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder6.AppendLine(ref handler);
            for (int i = 0; i < 4; i++)
            {
                stringBuilder2 = stringBuilder;
                StringBuilder stringBuilder7 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(10, 5, stringBuilder2);
                handler.AppendFormatted(value);
                handler.AppendLiteral("    ");
                handler.AppendFormatted(matrix4x[i, 0], 8, "0.000");
                handler.AppendLiteral(", ");
                handler.AppendFormatted(matrix4x[i, 1], 8, "0.000");
                handler.AppendLiteral(", ");
                handler.AppendFormatted(matrix4x[i, 2], 8, "0.000");
                handler.AppendLiteral(", ");
                handler.AppendFormatted(matrix4x[i, 3], 8, "0.000");
                stringBuilder7.AppendLine(ref handler);
            }

            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder8 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("</");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder8.AppendLine(ref handler);
            return;
        }

        if (propertyValue is sbyte)
        {
            object obj5 = propertyValue;
            sbyte value2 = (sbyte)((obj5 is sbyte) ? obj5 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder9 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value2);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value2, "x2");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder9.AppendLine(ref handler);
            return;
        }

        if (propertyValue is short)
        {
            object obj6 = propertyValue;
            short value3 = (short)((obj6 is short) ? obj6 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder10 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value3);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value3, "x4");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder10.AppendLine(ref handler);
            return;
        }

        if (propertyValue is int)
        {
            object obj7 = propertyValue;
            int value4 = (int)((obj7 is int) ? obj7 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder11 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value4);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value4, "x8");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder11.AppendLine(ref handler);
            return;
        }

        if (propertyValue is long)
        {
            object obj8 = propertyValue;
            long value5 = (long)((obj8 is long) ? obj8 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder12 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value5);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value5, "x16");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder12.AppendLine(ref handler);
            return;
        }

        if (propertyValue is byte)
        {
            object obj9 = propertyValue;
            byte value6 = (byte)((obj9 is byte) ? obj9 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder13 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value6);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value6, "x2");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder13.AppendLine(ref handler);
            return;
        }

        if (propertyValue is ushort)
        {
            object obj10 = propertyValue;
            ushort value7 = (ushort)((obj10 is ushort) ? obj10 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder14 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value7);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value7, "x4");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder14.AppendLine(ref handler);
            return;
        }

        if (propertyValue is uint)
        {
            object obj11 = propertyValue;
            uint value8 = (uint)((obj11 is uint) ? obj11 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder15 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value8);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value8, "x8");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder15.AppendLine(ref handler);
            return;
        }

        if (propertyValue is ulong)
        {
            object obj12 = propertyValue;
            ulong value9 = (ulong)((obj12 is ulong) ? obj12 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder16 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(9, 5, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(value9);
            handler.AppendLiteral("(");
            handler.AppendFormatted(value9, "x16");
            handler.AppendLiteral(") </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder16.AppendLine(ref handler);
            return;
        }

        if (propertyValue is bool)
        {
            object obj13 = propertyValue;
            bool flag = (bool)((obj13 is bool) ? obj13 : null);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder17 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(7, 4, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral("> ");
            handler.AppendFormatted(flag ? "True" : "False");
            handler.AppendLiteral(" </");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder17.AppendLine(ref handler);
            return;
        }

        if (!((object)propertyValue is Array array))
        {
            if ((object)propertyValue is Enum @enum)
            {
                stringBuilder2 = stringBuilder;
                StringBuilder stringBuilder18 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(8, 5, stringBuilder2);
                handler.AppendFormatted(value);
                handler.AppendLiteral("<");
                handler.AppendFormatted(propertyName);
                handler.AppendLiteral("> ");
                handler.AppendFormatted(@enum.GetType().Name);
                handler.AppendLiteral(".");
                handler.AppendFormatted(Enum.GetName(@enum.GetType(), @enum) ?? "Unknown");
                handler.AppendLiteral(" </");
                handler.AppendFormatted(propertyName);
                handler.AppendLiteral(">");
                stringBuilder18.AppendLine(ref handler);
                return;
            }

            if (propertyValue == null)
            {
                stringBuilder2 = stringBuilder;
                StringBuilder stringBuilder19 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(13, 3, stringBuilder2);
                handler.AppendFormatted(value);
                handler.AppendLiteral("<");
                handler.AppendFormatted(propertyName);
                handler.AppendLiteral("> (NULL) </");
                handler.AppendFormatted(propertyName);
                handler.AppendLiteral(">");
                stringBuilder19.AppendLine(ref handler);
                return;
            }

            string[] array2 = (propertyValue.ToString() ?? "").TrimEnd('\r', '\n').Split(Environment.NewLine);
            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder20 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("<");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder20.AppendLine(ref handler);
            string[] array3 = array2;
            foreach (string value10 in array3)
            {
                stringBuilder2 = stringBuilder;
                StringBuilder stringBuilder21 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(4, 2, stringBuilder2);
                handler.AppendFormatted(value);
                handler.AppendLiteral("    ");
                handler.AppendFormatted(value10);
                stringBuilder21.AppendLine(ref handler);
            }

            stringBuilder2 = stringBuilder;
            StringBuilder stringBuilder22 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
            handler.AppendFormatted(value);
            handler.AppendLiteral("</");
            handler.AppendFormatted(propertyName);
            handler.AppendLiteral(">");
            stringBuilder22.AppendLine(ref handler);
            return;
        }

        int num = 0;
        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder23 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(2, 2, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("<");
        handler.AppendFormatted(propertyName);
        handler.AppendLiteral(">");
        stringBuilder23.AppendLine(ref handler);
        foreach (object item in array)
        {
            stringBuilder.ConstructPropertyString(indentLevel + 2, $"Array:{num,3}", item);
            num++;
        }

        stringBuilder2 = stringBuilder;
        StringBuilder stringBuilder24 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(3, 2, stringBuilder2);
        handler.AppendFormatted(value);
        handler.AppendLiteral("</");
        handler.AppendFormatted(propertyName);
        handler.AppendLiteral(">");
        stringBuilder24.AppendLine(ref handler);
        stringBuilder.AppendLine("");
    }
}