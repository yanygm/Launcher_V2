using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KartLibrary.Text;

public class TextFormater
{
    private List<TextFormat> TextFormats = new List<TextFormat>();

    public int LevelDelta { get; set; } = 1;


    public void AddString(int Level, TextAlign align, string Text)
    {
        string[] array = Regex.Split(Text, "\\r\\n");
        foreach (string text in array)
        {
            TextFormats.Add(new TextFormat
            {
                Level = Level,
                Text = text,
                Align = align
            });
        }
    }

    public string StartFormat()
    {
        List<string> list = new List<string>();
        List<string> list2 = new List<string>();
        foreach (TextFormat textFormat in TextFormats)
        {
            switch (textFormat.Align)
            {
                case TextAlign.Top:
                    list.Add("".PadLeft(LevelDelta * textFormat.Level, ' ') + textFormat.Text);
                    break;
                case TextAlign.Bottom:
                    list2.Add("".PadLeft(LevelDelta * textFormat.Level, ' ') + textFormat.Text);
                    break;
            }
        }

        List<string> list3 = new List<string>();
        foreach (string item in list)
        {
            list3.Add(item);
        }

        list2.Reverse();
        foreach (string item2 in list2)
        {
            list3.Add(item2);
        }

        return string.Join("\r\n", list3);
    }
}