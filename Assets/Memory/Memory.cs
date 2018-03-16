using System;
using System.Runtime.InteropServices;

/// <summary>
/// C# API to the native memory plugin.
/// </summary>
public static class Memory
{
    /// <summary>
    /// Gets the managed memory in use.
    /// </summary>
    /// Convenience method; just a calls System.GC.GetTotalMemory.
    /// <value>The managed memory in use.</value>
    public static long managedUsed
    {
        get
        {
            return GC.GetTotalMemory(false);
        }
    }

    /// <summary>
    /// Gets the process total (native) resident memory.
    /// </summary>
    /// This only includes pages that are currently resident.
    /// On iOS in particular, in a memory pressure situation, resident
    /// will be very close to "dirty" memory.
    /// <value>The process resident memory in use.</value>
    public static long processResidentUsed
    {
        get
        {
            return ProcessResidentMemory();
        }
    }

    /// <summary>
    /// Gets the process total (native) virtual memory.
    /// </summary>
    /// This includes pages that are not resident.
    /// <value>The process virtual used.</value>
    public static long processVirtualUsed
    {
        get
        {
            return ProcessVirtualMemory();
        }
    }

    /// <summary>
    /// Gets the system free memory.
    /// </summary>
    /// <value>The system free memory.</value>
    public static long systemFree
    {
        get
        {
            return SystemFreeMemory();
        }
    }

    /// <summary>
    /// Gets the system total memory.
    /// </summary>
    /// <value>The system total memory.</value>
    public static long systemTotal
    {
        get
        {
            return SystemTotalMemory();
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

    [DllImport(__importName)] extern private static long SystemTotalMemory();
}
