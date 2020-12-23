using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PluginFileReader.API.Factory.Implementations.Delimited
{
    /// <summary>
    /// Class for writing CSV file
    /// </summary>
    public class DelimitedFileWriter : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly List<string> _fields = new List<string>();

        public char Delimiter { get; set; } = ',';
        public char QuoteChar { get; set; } = '"';
        public string CommentLineStartsWith { get; set; } = "#";
        public int NumberOfFields { get; set; } = 0;
        public bool QuoteWrap { get; set; } = true;
        public string NullValue { get; set; } = "null";

        public DelimitedFileWriter(string path)
        {
            _writer = new StreamWriter(path, true, Encoding.UTF8);
            if (!_writer.BaseStream.CanWrite)
                throw new Exception("Stream does not support writing.");
        }

        public DelimitedFileWriter(string path, Encoding encoding)
        {
            _writer = new StreamWriter(path, true, encoding);
            if (!_writer.BaseStream.CanWrite)
                throw new Exception("Stream does not support writing.");
        }

        public DelimitedFileWriter(string path, bool append, Encoding encoding)
        {
            _writer = new StreamWriter(path, append, encoding);
            if (!_writer.BaseStream.CanWrite)
                throw new Exception("Stream does not support writing.");
        }

        public void AddField(string columnValue)
        {
            string value;
            if (columnValue == null)
            {
                value = QuoteWrap ? $"\"{NullValue}\"" : NullValue;
            }
            else
            {
                value = QuoteValue(columnValue);
            }

            _fields.Add(value);
        }

        public void AddField(params string[] columnValues)
        {
            foreach (string columnValue in columnValues)
                AddField(columnValue);
        }

        public void AddComments(string comments)
        {
            if (string.IsNullOrWhiteSpace(CommentLineStartsWith))
                throw new Exception("Property CommentLineStartsWith must be set before comments can be added");

            string[] newLineChars = {"\n", "\r", "\r\n", "\n\r"};
            foreach (string line in comments.Split(newLineChars, StringSplitOptions.None))
            {
                _writer.WriteLine(CommentLineStartsWith + line);
            }
        }

        public void WriteLineToFile(string line)
        {
            _writer.AutoFlush = true;
            _writer.WriteLine(line);
            _writer.Flush();
            _writer.BaseStream.Flush();
            System.Threading.Thread.Sleep(1);
        }

        public void SaveAndCommitLine()
        {
            _writer.AutoFlush = true;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _fields.Count; i++)
            {
                sb.Append(_fields[i]);

                if ((NumberOfFields > 0) && (i >= NumberOfFields))
                    break;

                if (i + 1 < _fields.Count)
                    sb.Append(Delimiter);
            }

            if ((NumberOfFields > 0) && (NumberOfFields > _fields.Count))
            {
                for (int i = _fields.Count; i < NumberOfFields; i++)
                {
                    if (i + 1 < NumberOfFields)
                        sb.Append(Delimiter);
                }
            }

            _writer.WriteLine(sb.ToString());
            _writer.Flush();
            _writer.BaseStream.Flush();
            System.Threading.Thread.Sleep(1);
            _fields.Clear();
        }

        private string QuoteValue(string value)
        {
            if (value.IndexOf(QuoteChar) > -1)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(QuoteChar);
                sb.Append(QuoteChar);
                value = value.ToString().Replace(QuoteChar.ToString(), sb.ToString());
            }

            return QuoteWrap ? $"{QuoteChar}{value}{QuoteChar}" : value;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}