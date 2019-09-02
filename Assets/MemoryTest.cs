using System.Text;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MemoryTest : MonoBehaviour
{
	public Text text;

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

		{
			long memory = Memory.processResidentUsed;
			_builder.Append("Process Resident: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

			memory = Memory.processVirtualUsed;
			_builder.Append("Process Virtual: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

			memory = Memory.systemFree;
			_builder.Append("System Free: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

			memory = Memory.systemTotal;
			_builder.Append("System Total: ").Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();
		}

		{
			var path = Application.persistentDataPath;

			var size = Memory.TotalSize(path);
			_builder.Append("FS Total: ").Append(size / BYTE_TO_MEGABYTE).Append('.').Append((size % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

			size = Memory.FreeSize(path);
			_builder.Append("FS Free: ").Append(size / BYTE_TO_MEGABYTE).Append('.').Append((size % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();

			size = Memory.AvailableSize(path);
			_builder.Append("FS Available: ").Append(size / BYTE_TO_MEGABYTE).Append('.').Append((size % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB").AppendLine();
		}

		text.text = _builder.ToString();
	}
}
