using System.Text;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MemoryTest : MonoBehaviour
{
	public Text text;

	private StringBuilder _builder = new StringBuilder();
	private long _lastMemoryKB;
	private long BYTE_TO_KILOBYTE = 1000;
	private long BYTE_TO_MEGABYTE = 1000 * 1000;

	private void OnEnable()
	{
		if(null == text)
		{
			enabled = false;
			return;
		}

		_lastMemoryKB = -1;
	}

	private void Update()
	{
		long memory = Memory.processResidentUsed;
		long memoryKB = (memory + BYTE_TO_KILOBYTE - 1) / BYTE_TO_KILOBYTE;
		if(_lastMemoryKB != memoryKB)
		{
			_lastMemoryKB = memoryKB;
			_builder.Length = 0;
			_builder.Append(memory / BYTE_TO_MEGABYTE).Append('.').Append((memory % BYTE_TO_MEGABYTE) / (BYTE_TO_MEGABYTE / 10)).Append("MB");
			text.text = _builder.ToString();
		}
	}
}
