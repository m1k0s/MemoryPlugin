using System.Text;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MemoryTest : MonoBehaviour
{
    public Text text;

    private string _path;
    private readonly StringBuilder _builder = new StringBuilder();
    private const long BYTE_TO_MEBIBYTE = 1024 * 1024;

    private void OnEnable()
    {
        _path = Application.persistentDataPath;

        if (null == text)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        _builder.Length = 0;

        {
            long memory = Memory.processResidentUsed;
            _builder.Append("Process Resident: ").Append(memory / BYTE_TO_MEBIBYTE).Append('.').Append((memory % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();

            memory = Memory.processVirtualUsed;
            _builder.Append("Process Virtual: ").Append(memory / BYTE_TO_MEBIBYTE).Append('.').Append((memory % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();

            memory = Memory.systemFree;
            _builder.Append("System Free: ").Append(memory / BYTE_TO_MEBIBYTE).Append('.').Append((memory % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();

            memory = Memory.systemTotal;
            _builder.Append("System Total: ").Append(memory / BYTE_TO_MEBIBYTE).Append('.').Append((memory % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();
        }

        {
            var size = Memory.TotalSize(_path);
            _builder.Append("FS Total: ").Append(size / BYTE_TO_MEBIBYTE).Append('.').Append((size % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();

            size = Memory.FreeSize(_path);
            _builder.Append("FS Free: ").Append(size / BYTE_TO_MEBIBYTE).Append('.').Append((size % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();

            size = Memory.AvailableSize(_path);
            _builder.Append("FS Available: ").Append(size / BYTE_TO_MEBIBYTE).Append('.').Append((size % BYTE_TO_MEBIBYTE) / (BYTE_TO_MEBIBYTE / 10)).Append("MiB").AppendLine();
        }

        text.text = _builder.ToString();
    }
}
