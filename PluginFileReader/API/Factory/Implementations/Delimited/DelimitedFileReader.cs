using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PluginFileReader.API.Utility;
using PluginFileReader.Helper;

namespace PluginFileReader.API.Factory.Implementations.Delimited
{
    /// <summary>
    /// Class for reading from comma separated values (CSV) file
    /// </summary>
    public class DelimitedFileReader : IDisposable
    {
        // Private members 
        private readonly StreamWrapper _streamWrapper;
        private readonly StreamReader _reader;
        private string _currentLineText;
        private int _currentPosition;
        private int _lineCount = 0;
        private BlankLine _onEmptyLine = BlankLine.SkipEntireLine;
        readonly List<string> _columns = new List<string>();
        readonly List<string> _comments = new List<string>();
        private bool _isHeaderSkipped = false;
        private readonly HashSet<int> _columnIndexes = new HashSet<int>();
        private long _totalLength = 0;
        private readonly List<long> _lineOffSets = new List<long>();
        private bool _isLineEmtpy = false;

        public string[] Fields => _columns.ToArray();

        public string[] CommentLines => _comments.ToArray();
        public char Delimiter { get; set; } = Convert.ToChar(",");
        public char QuoteChar { get; set; } = Convert.ToChar('"');
        public string CommentLineStartsWith { get; set; } = "";

        public HashSet<int> ColumnIndexes
        {
            get { return _columnIndexes; }
        }

        public int SkipLines { get; set; } = 0;
        public int MaximumLines { get; set; } = -1;
        public bool StoreLineOffSets { get; set; } = false;
        public long StartAtOffSet { get; set; } = 0;

        public List<long> LineOffSets
        {
            get { return _lineOffSets; }
        }

        public int LineCount
        {
            get { return _lineCount; }
        }

        public long CurrentOffSet
        {
            get { return _totalLength; }
        }

        public BlankLine OnEmptyLine
        {
            get { return _onEmptyLine; }
            set { _onEmptyLine = value; }
        }

        public bool IsLineEmpty
        {
            get { return _isLineEmtpy; }
        }

        private void InitCsvReader()
        {
            _lineOffSets.Clear();
            StartAtOffSet = 0;
            _lineCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the DelimitedFileReader class for the
        /// specified file path.
        /// </summary>
        /// <param name="path">The name of the CSV file to read from</param>
        /// <param name="rootPath">The root path object/param>
        public DelimitedFileReader(string path, RootPathObject rootPath)
        {
            InitCsvReader();
            _streamWrapper = Utility.Utility.GetStream(path, rootPath.FileReadMode);
            _reader = _streamWrapper.StreamReader;
        }

        public void RestrictToColumns(params int[] columnIndexes)
        {
            foreach (int i in columnIndexes)
            {
                _columnIndexes.Add(i);
            }
        }

        /// <summary>
        /// Reads a row of columns from the current CSV file. Returns false if no
        /// more data could be read because the end of the file was reached.
        /// </summary>        
        public bool ReadLine()
        {
            if (MaximumLines > 0)
            {
                if (_lineCount >= MaximumLines)
                    return false;
            }

            if ((!_isHeaderSkipped) && (SkipLines > 0))
            {
                int rowsSkipped = 0;
                while (!_isHeaderSkipped)
                {
                    //_Reader.ReadLine();
                    ReadNextLine();
                    rowsSkipped++;
                    if (rowsSkipped >= SkipLines)
                    {
                        _isHeaderSkipped = true;
                        break;
                    }
                }
            }

            // Read next line from the file
            if ((_currentLineText = ReadNextLine()) == null)
                return false;


            _isLineEmtpy = false;

            _currentPosition = 0;

            // Test for empty line
            if (_currentLineText.Length == 0)
            {
                _isLineEmtpy = true;
                switch (_onEmptyLine)
                {
                    case BlankLine.EmptySingleColumn:
                        _columns.Clear();
                        return true;
                    case BlankLine.SkipEntireLine:
                        return ReadLine();
                    case BlankLine.EndOfFile:
                        return false;
                }
            }
            else
            {
                _columns.Clear();
            }

            // Parse line            
            int columnCount = 0;
            while (true)
            {
                string column;

                // Read next column
                if (_currentPosition < _currentLineText.Length && _currentLineText[_currentPosition] == QuoteChar)
                    column = ReadQuotedColumn();
                else
                    column = ReadUnquotedColumn();

                if (_columnIndexes.Count > 0)
                {
                    if (_columnIndexes.Contains(columnCount))
                    {
                        _columns.Add(column);
                        if (_columns.Count == _columnIndexes.Count)
                            break;
                    }
                }
                else
                {
                    _columns.Add(column);
                }

                columnCount++;

                // Break if we reached the end of the line
                if (_currentLineText == null || _currentPosition == _currentLineText.Length)
                    break;
                // Otherwise skip delimiter
                if (_currentLineText[_currentPosition].Equals(Delimiter))
                    _currentPosition++;
            }

            // Indicate success
            _lineCount++;
            return true;
        }


        private string ReadNextLine(bool isContinued = false)
        {
            string lineText;
            if (!_reader.EndOfStream)
            {
                if ((!isContinued) && (StoreLineOffSets) && (!_lineOffSets.Contains(_totalLength)))
                {
                    _lineOffSets.Add(_totalLength);
                }

                if (StartAtOffSet > 0)
                {
                    _reader.DiscardBufferedData();
                    _reader.BaseStream.Seek(StartAtOffSet, SeekOrigin.Begin);
                    StartAtOffSet = -1;
                }

                if (CommentLineStartsWith.Trim().Length > 0)
                {
                    lineText = _reader.ReadLine();
                    while (lineText != null && lineText.Trim().StartsWith(CommentLineStartsWith))
                    {
                        _totalLength += lineText.Length + Environment.NewLine.Length;
                        _comments.Add(lineText);
                        lineText = _reader.ReadLine();
                    }
                }
                else
                {
                    lineText = _reader.ReadLine();
                }

                if (lineText != null)
                    _totalLength += lineText.Length + Environment.NewLine.Length;
            }
            else
            {
                return null;
            }

            return lineText;
        }

        /// <summary>
        /// Reads a quoted column by reading from the current line until a
        /// closing quote is found or the end of the file is reached. 
        /// </summary>
        private string ReadQuotedColumn()
        {
            // Skip opening quote character
            if (_currentPosition < _currentLineText.Length && _currentLineText[_currentPosition] == QuoteChar)
                _currentPosition++;

            // Parse column
            StringBuilder builder = new StringBuilder();
            while (true)
            {
                while (_currentPosition == _currentLineText.Length)
                {
                    // End of line so attempt to read the next line
                    _currentLineText = ReadNextLine(true); //_Reader.ReadLine();

                    _currentPosition = 0;
                    // Done if we reached the end of the file
                    if (_currentLineText == null)
                        return builder.ToString();
                    // Otherwise, treat as a multi-line field
                    builder.Append(Environment.NewLine);
                }

                // check for quote character
                if (_currentLineText[_currentPosition] == QuoteChar)
                {
                    // If two quotes, skip first and treat second as literal
                    int nextPos = (_currentPosition + 1);
                    if (nextPos < _currentLineText.Length && _currentLineText[nextPos] == QuoteChar)
                        _currentPosition++;
                    else
                        break; // Single quote ends quoted sequence
                }

                // Add current character to the column
                builder.Append(_currentLineText[_currentPosition++]);
            }

            if (_currentPosition < _currentLineText.Length)
            {
                // Consume closing quote
                if (_currentLineText[_currentPosition] == QuoteChar)
                    _currentPosition++;

                // Append any additional characters appearing before next delimiter
                builder.Append(ReadUnquotedColumn());
            }

            // Return column value
            return builder.ToString();
        }

        /// <summary>
        /// Reads an unquoted column by reading from the current line until a
        /// delimiter is found or the end of the line is reached. 
        /// </summary>
        private string ReadUnquotedColumn()
        {
            int startPos = _currentPosition;
            if ((_currentPosition = _currentLineText.IndexOf(Delimiter, _currentPosition)) == -1)
                _currentPosition = _currentLineText.Length;

            if (_currentPosition > startPos)
                return _currentLineText.Substring(startPos, _currentPosition - startPos);
            return String.Empty;
        }


        public void Dispose()
        {
            _reader.Dispose();
            _streamWrapper.Close();
        }
    }
}