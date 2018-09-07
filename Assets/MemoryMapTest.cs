using System.Text;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
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
    private static readonly char[] HEX_DIGITS = new char[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
    };

    private long _lastMappedFileDataOffset = -1;

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
        _lastMappedFileDataOffset = -1;

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

    private void Update()
    {
        if (-1 == _mappedFileDataStart || _lastMappedFileDataOffset == _mappedFileDataOffset || !Application.isPlaying)
        {
            return;
        }

        _builder.Length = 0;
        _builder.AppendLine(file);

        // Work out which part of the file we are reading.
        long displayStart = _mappedFileDataStart + _mappedFileDataOffset;
        long pageStart = displayStart;
        long pageEnd = pageStart + _buffer.Length;
        if (pageEnd > _mappedFileDataEnd)
        {
            pageEnd = _mappedFileDataEnd;
        }

        // Copy a buffer's worth of data from the pointer.
        System.Runtime.InteropServices.Marshal.Copy(new System.IntPtr(displayStart), _buffer, 0, (int)(pageEnd - pageStart));

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

            _builder.Append(pageStart).Append(":");

            for (; lineStart < lineEnd; ++lineStart)
            {
                uint c = (uint)_buffer[(int)(lineStart - displayStart)];
                _builder.Append(' ');
                _builder.Append(HEX_DIGITS[c / 16]);
                _builder.Append(HEX_DIGITS[c % 16]);
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
