using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MemoryMapTest : MonoBehaviour
{
    public Text text;
    public string file;

    private StringBuilder _builder = new StringBuilder();

    private Memory.MappedFile _mappedFile;
    private long _mappedFileDataStart = -1;
    private long _mappedFileDataEnd = -1;
    private long _mappedFileDataOffset = 0;
    private byte[] _buffer = new byte[512];

    private long _lastDisplayStart = -1;

    private void OnEnable()
    {
        if (null == text)
        {
            enabled = false;
            return;
        }

        text.text = string.Empty;

        _mappedFileDataStart = -1;
        _mappedFileDataEnd = -1;
        _lastDisplayStart = -1;

#if UNITY_EDITOR
        var path = Application.dataPath + "/StreamingAssets/";
#elif UNITY_IOS
        var path = Application.dataPath + "/Raw/";
#elif UNITY_ANDROID
        var path = "jar:file://" + Application.dataPath + "!/assets/";
#endif
        _mappedFile = Memory.MappedFile.CreateFromFile(path + file);
        if (null != _mappedFile && System.IntPtr.Zero != _mappedFile.data)
        {
            _mappedFileDataStart = _mappedFile.data.ToInt64();
            _mappedFileDataEnd = _mappedFileDataStart + _mappedFile.size;
            _mappedFileDataOffset = 0;
            Redraw();
        }
    }

    private void OnDisable()
    {
        if (null != _mappedFile)
        {
            _mappedFile.Dispose();
            _mappedFile = null;
            _mappedFileDataStart = -1;
            _mappedFileDataEnd = -1;
        }

        if (null != text)
        {
            text.text = string.Empty;
        }
    }

    public void Up()
    {
        if (-1 == _mappedFileDataStart)
        {
            return;
        }

        _mappedFileDataOffset -= _buffer.Length;
        if (_mappedFileDataOffset < 0)
        {
            _mappedFileDataOffset = 0;
        }

        Redraw();
    }

    public void Down()
    {
        if (-1 == _mappedFileDataStart)
        {
            return;
        }

        _mappedFileDataOffset += _buffer.Length;
        if (_mappedFileDataOffset > _mappedFile.size)
        {
            _mappedFileDataOffset = (_mappedFile.size / _buffer.Length) * _buffer.Length;
        }

        Redraw();
    }

    private void Redraw()
    {
        long displayStart = _mappedFileDataStart + _mappedFileDataOffset;
        if (_lastDisplayStart == displayStart)
        {
            return;
        }
        _lastDisplayStart = displayStart;

        // Work out which part of the file we are reading.
        long pageStart = displayStart;
        long pageEnd = pageStart + _buffer.Length;
        if (pageEnd > _mappedFileDataEnd)
        {
            pageEnd = _mappedFileDataEnd;
        }

        // Copy a buffer's worth of data from the pointer.
        System.Runtime.InteropServices.Marshal.Copy(new System.IntPtr(displayStart), _buffer, 0, (int)(pageEnd - pageStart));

        // Prepare the string builder.
        _builder.Length = 0;
        _builder.Append("File: ").Append(file).AppendLine();
        _builder.Append("Size: ").Append(_mappedFile.size).AppendLine();

        // Add it to the string builder a "line" at a time.
        const long LINE_LENGTH = 16;
        for (; pageStart < pageEnd; pageStart += LINE_LENGTH)
        {
            long lineStart = pageStart;
            long lineEnd = lineStart + LINE_LENGTH;
            long lineEndUnclamped = lineEnd;
            if (lineEnd > _mappedFileDataEnd)
            {
                lineEnd = _mappedFileDataEnd;
            }

            _builder.AppendHex((ulong)(pageStart - _mappedFileDataStart), 8).Append(':');

            for (; lineStart < lineEnd; ++lineStart)
            {
                _builder.Append(' ').AppendHex(_buffer[(int)(lineStart - displayStart)], 2);
            }

            for (; lineStart < lineEndUnclamped; ++lineStart)
            {
                _builder.Append(" __");
            }

            _builder.Append(" | ");

            for (lineStart = pageStart; lineStart < lineEnd; ++lineStart)
            {
                uint c = (uint)_buffer[(int)(lineStart - displayStart)];
                _builder.Append(c >= 32 && c <= 127 ? (char)c : '.');
            }

            _builder.AppendLine();
        }

        text.text = _builder.ToString();
    }
}

internal static class StringBuilderExtensions
{

    private static readonly char[] HEX_DIGITS = new char[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
    };

    private static Stack<char> _buffer = new Stack<char>(16);

    public static StringBuilder AppendHex(this StringBuilder sb, ulong n, int fieldSize = 0)
    {
        _buffer.Clear();

        do
        {
            _buffer.Push(HEX_DIGITS[n % 16]);
            n /= 16;
            --fieldSize;
        }
        while(n > 0);

        while (fieldSize-- > 0)
        {
            sb.Append(HEX_DIGITS[0]);
        }
        foreach (var c in _buffer)
        {
            sb.Append(c);
        }

        return sb;
    }
}
