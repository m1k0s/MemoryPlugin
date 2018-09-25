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
    /// On iOS/OSX this means dirty + compressed.
    /// On Android this means VmRSS.
    /// On Windows this means WorkingSetSize.
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

    /// <summary>
    /// C# class representing a memory-mapped file.
    /// </summary>
    public class MappedFile : IDisposable
    {
        /// <summary>
        /// Gets the data pointer to the memory-mapped file contents.
        /// </summary>
        /// This can be IntPtr.Zero.
        /// <value>The data.</value>
        public IntPtr data
        {
            get
            {
                return _data;
            }
        }

        /// <summary>
        /// Gets the size of the memory-mapped file.
        /// </summary>
        /// <value>The size.</value>
        public long size
        {
            get
            {
                return _size;
            }
        }

        private IntPtr _handle;
        private IntPtr _data = IntPtr.Zero;
        private long _size = 0;
        private bool _disposed = false;

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="Memory+MappedFile"/> is reclaimed by garbage collection.
        /// </summary>
        ~MappedFile()
        {
            Dispose(false);
        }

        /// <summary>
        /// Creates a <see cref="Memory+MappedFile"/> instance from a file path.
        /// </summary>
        /// <returns>The <see cref="Memory+MappedFile"/>.</returns>
        /// <param name="path">Path to the file on-disk.</param>
        public static MappedFile CreateFromFile(string path)
        {
            return CreateFromFile(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        }

        /// <summary>
        /// Creates a <see cref="Memory+MappedFile"/> instance from a file path.
        /// </summary>
        /// <returns>The <see cref="Memory+MappedFile"/>.</returns>
        /// <param name="path">Path to the file on-disk.</param>
        /// <param name="mode">Mode. Currently only <see cref="FileMode+Open"/> is supported.</param>
        /// <param name="access">Access. Currently only <see cref="FileAccess+Read"/> is supported.</param>
        public static MappedFile CreateFromFile(string path, System.IO.FileMode mode, System.IO.FileAccess access)
        {
            if (path == null)
            {
                throw new ArgumentNullException();
            }

            if (!System.IO.File.Exists(path))
            {
                throw new System.IO.IOException(string.Concat("\"", path, "\" does not exist."));
            }

            if (mode != System.IO.FileMode.Open)
            {
                throw new NotSupportedException("Only FileMode.Open is supported.");
            }

            if (access != System.IO.FileAccess.Read)
            {
                throw new NotSupportedException("Only FileAccess.Read is supported.");
            }

            var mappedFile = new MappedFile();

            mappedFile._handle = MemoryMap(path, out mappedFile._data, out mappedFile._size);
            if (mappedFile._size == -1)
            {
                throw new System.IO.IOException(string.Concat("Failed to memory-map \"", path, "\"."));
            }

            return mappedFile;
        }

        /// <summary>
        /// Public implementation of Dispose pattern callable by consumers.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Memory+MappedFile"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Memory+MappedFile"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="Memory+MappedFile"/> so the garbage
        /// collector can reclaim the memory that the <see cref="Memory+MappedFile"/> was occupying.</remarks>
        public void Dispose()
        { 
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c> disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                MemoryUnMap(_handle, _data, _size);
            }

            _disposed = true;
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

    [DllImport(__importName)] extern private static IntPtr MemoryMap(string path, out IntPtr data, out long size);

    [DllImport(__importName)] extern private static void MemoryUnMap(IntPtr handle, IntPtr data, long size);
}
