using System.Text;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MemoryTest : MonoBehaviour
{
    public Text text;
    public string file;

    private StringBuilder _builder = new StringBuilder();
    private const long BYTE_TO_MEGABYTE = 1000 * 1000;

    private void OnEnable()
    {
        if (null == text)
        {
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        _builder.Length = 0;

        long memory = Memory.processResidentUsed;
        _builder.Append("Process Resident: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

        memory = Memory.processVirtualUsed;
        _builder.Append("Process Virtual: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

        memory = Memory.systemFree;
        _builder.Append("System Free: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

        memory = Memory.systemTotal;
        _builder.Append("System Total: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

        #if UNITY_EDITOR
        var path = Application.dataPath + "/StreamingAssets/";
        #elif UNITY_IOS
        var path = Application.dataPath + "/Raw/";
        #elif UNITY_ANDROID
        var path = "jar:file://" + Application.dataPath + "!/assets/";
        #endif
        path += file;
        using(var file = Memory.MappedFile.CreateFromFile(path))
        {
            _builder.AppendLine(path);
            _builder.Append("Data: ").Append(file.data).AppendLine();
            _builder.Append("Size: ").Append(file.size).AppendLine();
        }

        text.text = _builder.ToString();
    }
}
