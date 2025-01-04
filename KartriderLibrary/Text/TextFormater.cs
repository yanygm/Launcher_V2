using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace KartLibrary.Text
{
    public class TextFormater
    {
        public int LevelDelta { get; set; } = 1;

        private List<TextFormat> TextFormats = new List<TextFormat>();

        
        public void AddString(int Level, TextAlign align,string Text)
        {
            string[] Lines = Regex.Split(Text, "\\r\\n");
            foreach(string Line in Lines)
            {
                TextFormats.Add(new TextFormat()
                {
                    Level = Level,
                    Text = Line,
                    Align = align
                });
            }
        }

        public string StartFormat()
        {
            List<string> TopLine = new List<string>();
            List<string> BottomLine = new List<string>();
            foreach(TextFormat tf in TextFormats)
            {
                switch (tf.Align)
                {
                    case TextAlign.Top:
                        TopLine.Add($"{"".PadLeft(LevelDelta*tf.Level,' ')}{tf.Text}");
                        break;
                    case TextAlign.Bottom:
                        BottomLine.Add($"{"".PadLeft(LevelDelta * tf.Level, ' ')}{tf.Text}");
                        break;
                }
            }
            List<string> output = new List<string>();
            foreach(string tl in TopLine)
            {
                output.Add(tl);
            }
            BottomLine.Reverse();
            foreach (string tl in BottomLine)
            {
                output.Add(tl);
            }
            return string.Join("\r\n", output);
        }
    }

    public struct TextFormat
    {
        public int Level { get; set; }

        public string Text { get; set; }

        public TextAlign Align { get; set; }


    }

    public enum TextAlign
    {
        Top,Bottom
    }
}
