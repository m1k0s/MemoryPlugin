using System;
using System.Runtime.InteropServices;

public static class Memory
{
	public static long managedUsed
	{
		get
		{
			return GC.GetTotalMemory(false);
		}
	}

	public static long processResidentUsed
	{
		get
		{
			return ProcessResidentMemory();
		}
	}

	public static long processVirtualUsed
	{
		get
		{
			return ProcessVirtualMemory();
		}
	}

	public static long systemFree
	{
		get
		{
			return SystemFreeMemory();
		}
	}

	#if UNITY_IPHONE && !UNITY_EDITOR
	private const string __importName = "__Internal";
	#else
	private const string __importName = "memory";
	#endif

	[DllImport(__importName)] extern private static long ProcessResidentMemory();

	[DllImport(__importName)] extern private static long ProcessVirtualMemory();

	[DllImport(__importName)] extern private static long SystemFreeMemory();
}
