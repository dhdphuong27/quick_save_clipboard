using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Quicksave_Clipboard.MVVM.Model
{
    public class TextClipboardContent : ClipboardContent
    {
        private string _fullText;
        private string _previewText;

        public string FullText
        {
            get { return _fullText; }
            set
            {
                if (_fullText != value)
                {
                    _fullText = value;
                    OnPropertyChanged(nameof(FullText));

                    // Update PreviewText whenever FullText changes
                    PreviewText = GetPreviewText(_fullText);
                }
            }
        }
        public string PreviewText
        {
            get { return _previewText; }
            private set
            {
                if (_previewText != value)
                {
                    _previewText = value;
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }
        public TextClipboardContent(string text) : base()
        {
            FullText = text;
            PreviewText = GetPreviewText(text);
            ContentType = Type.Text;
        }
        public TextClipboardContent(string text, string dateCreated) : base(dateCreated)
        {
            FullText = text;
            PreviewText = GetPreviewText(text);
            ContentType = Type.Text;
        }
        private string GetPreviewText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
            // Limit preview to 100 characters
            return text.Length > 100 ? text.Substring(0, 100) + "..." : text;
        }

        public override void SaveToFile()
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ClipboardHistory");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            ////add random string everytime it's saved so that it wouldn't collide if user double click save file
            //const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            //StringBuilder randomStringBuilder = new StringBuilder(3);
            //Random random = new Random();
            //for (int i = 0; i < 3; i++)
            //{
            //    randomStringBuilder.Append(chars[random.Next(chars.Length)]);
            //}

            string fileName = ID + ".txt";

            string filePath = Path.Combine(directoryPath, fileName);
            File.WriteAllText(filePath, FullText);
        }
    }
}
