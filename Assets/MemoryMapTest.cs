using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MemoryMapTest : MonoBehaviour
{
    public Text text;
    public string file;

    private readonly StringBuilder _builder = new StringBuilder();

    private string _tmpFile = null;
    private Memory.MappedFile _mappedFile;
    private long _mappedFileDataStart = -1;
    private long _mappedFileDataEnd = -1;
    private long _mappedFileDataOffset = 0;
    private readonly byte[] _buffer = new byte[512];

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

        StartCoroutine(MemoryMapFile());
    }

    private IEnumerator MemoryMapFile()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, file);
        Debug.LogFormat("MemoryMapTest: {0}", path);

        if (path.Contains("://"))
        {
            // StreamingAssets on Android is packed in the compressed jar so we can't mmap it directly...
            // Use UnityWebRequest to copy to a tmp file first.
            UnityWebRequest req = UnityWebRequest.Get(path);

            // We can't use System.IO.Path.GetTempFileName() on Android either...
            path = System.IO.Path.Combine(Application.temporaryCachePath, System.IO.Path.GetRandomFileName());
            Debug.LogFormat("MemoryMapTest: {0}", path);

            // Kick off the async operation saving the file directly to disk.
            req.downloadHandler = new DownloadHandlerFile(path);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.ProtocolError ||
                req.result == UnityWebRequest.Result.DataProcessingError)
            {
                throw new System.IO.IOException(req.error);
            }
        }

        _mappedFile = Memory.MappedFile.CreateFromFile(path);
        if (null != _mappedFile && System.IntPtr.Zero != _mappedFile.data)
        {
            _mappedFileDataStart = _mappedFile.data.ToInt64();
            _mappedFileDataEnd = _mappedFileDataStart + _mappedFile.size;
            _mappedFileDataOffset = 0;
            Redraw();
        }

        yield return null;
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

        if (null != _tmpFile)
        {
            System.IO.File.Delete(_tmpFile);
            _tmpFile = null;
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
        _builder.Append("Size: ").Append(_mappedFile.size).Append(" (").Append((ulong)_mappedFile.size, 8).AppendLine(")");

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

            _builder.Append((ulong)(pageStart - _mappedFileDataStart), 8).Append(" |");

            for (; lineStart < lineEnd; ++lineStart)
            {
                _builder.Append(' ').Append(_buffer[(int)(lineStart - displayStart)], 2);
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
    private static readonly char[] RADIX_DIGITS = new char[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F',
        'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V'
    };

    private static readonly Stack<char> _buffer = new Stack<char>(16);

    public static StringBuilder Append(this StringBuilder sb, ulong n, int fieldSize = 0, ulong radix = 16)
    {
        _buffer.Clear();

        do
        {
            _buffer.Push(RADIX_DIGITS[n % radix]);
            n /= radix;
            --fieldSize;
        }
        while (n > 0);

        while (fieldSize-- > 0)
        {
            sb.Append(RADIX_DIGITS[0]);
        }
        foreach (var c in _buffer)
        {
            sb.Append(c);
        }

        return sb;
    }
}
