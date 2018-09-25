#include <stdint.h>

#if defined(__MACH__)
#include <mach/mach.h>
#include <stdio.h>
#include <sys/mman.h>
#include <sys/stat.h>
#elif defined(__ANDROID__)
#include <fcntl.h>
#include <stdlib.h>
#include <android/log.h>
#include <stdio.h>
#include <sys/mman.h>
#include <sys/stat.h>
#elif defined(_WIN32)
#include "windows.h"
#include "psapi.h"
#endif

#if defined(_WIN32) && !defined(__SCITECH_SNAP__)
#   define PLUGIN_APICALL __declspec(dllexport)
#elif defined(__ANDROID__)
#   include <sys/cdefs.h>
#   define PLUGIN_APICALL __attribute__((visibility("default"))) __NDK_FPABI__
#else
#   define PLUGIN_APICALL
#endif

#if defined(_WIN32) && !defined(_WIN32_WCE) && !defined(__SCITECH_SNAP__)
#   define PLUGIN_APIENTRY __stdcall
#else
#   define PLUGIN_APIENTRY
#endif

#if defined(__ANDROID__)
#define KIBIBYTES_TO_BYTES 1024

/**
 *	Parse a proc fs file for (an array of) label/number pairs and return their sum.
 *	Based on https://android.googlesource.com/platform/frameworks/base.git/+/master/core/jni/android_util_Process.cpp
 *
 *	@param	caller	Calling function name for logging.
 *	@param	file	The proc fs file to use.
 *	@param	sums	Array of labels to search for.
 *	@param	sumsLen	Array of label sizes (one per label in sums).
 *	@param	num		Array size.
 *
 *	@return	The calculated sum.
 */
static int64_t getMemoryImpl(const char* const caller, const char* const file, const char* const sums[], const size_t sumsLen[], size_t num)
{
	int fd = open(file, O_RDONLY | O_CLOEXEC);
	if(fd < 0)
	{
		__android_log_print(ANDROID_LOG_DEBUG, caller, "Unable to open %s\n", file);
		return -1;
	}
	
	char buffer[512];
	const ssize_t len = read(fd, buffer, sizeof(buffer) - 1);
	close(fd);
	if(len < 0)
	{
		__android_log_print(ANDROID_LOG_DEBUG, caller, "Unable to read %s\n", file);
		return -1;
	}
	
	// 0 terminate the buffer we just read.
	buffer[len] = 0;

	// This will keep track of the total memory parsed.
	int64_t mem = 0;
	
	// Keep track of the number of sum labels found.
	size_t numFound = 0;
	
	for(char* p = buffer; *p && numFound < num; ++p)
	{
		for(size_t i = 0; sums[i]; ++i)
		{
			// Does the sum label match.
			if (strncmp(p, sums[i], sumsLen[i]) == 0)
			{
				// It does, increment how many we found.
				numFound++;

				// Skip the label size.
				p += sumsLen[i];
				
				// Skip whitespace.
				while(*p == ' ' || *p == '\t') ++p;
				
				// Number start.
				char* num = p;
				
				// Find the number end.
				while (*p >= '0' && *p <= '9') ++p;
				
				// If not at the buffer end.
				if(*p != 0)
				{
					// 0 terminate the number string.
					*p++ = 0;
					
					// Just make sure we are not at the buffer end.
					if(*p == 0) --p;
				}
				
				// Convert the number string to an actual number and add it to the total.
				mem += atoll(num);
				
				// Label found; break out of the inner loop.
				break;
			}
		}
	}
	
	return numFound > 0 ? mem : -1;
}
#endif

/**
 *	Return the process resident memory.
 *
 *	@return	The process resident memory in bytes.
 */
extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY ProcessResidentMemory()
{
#if defined(__MACH__)
    mach_port_t task = mach_task_self();
	struct task_vm_info info;
	mach_msg_type_number_t size = TASK_VM_INFO_COUNT;
	if(KERN_SUCCESS == task_info(task, TASK_VM_INFO, (task_info_t)&info, &size))
	{
		return static_cast<int64_t>(info.internal + info.compressed);
	}
#elif defined(__ANDROID__)
	static const char* const sums[] = { "VmRSS:", NULL };
	static const size_t sumsLen[] = { strlen("VmRSS:"), 0 };
	return getMemoryImpl(__FUNCTION__, "/proc/self/status", sums, sumsLen, 1) * KIBIBYTES_TO_BYTES;
#elif defined(_WIN32)
	PROCESS_MEMORY_COUNTERS_EX pmc;
	GetProcessMemoryInfo(GetCurrentProcess(), reinterpret_cast<PROCESS_MEMORY_COUNTERS*>(&pmc), sizeof(pmc));
	return static_cast<int64_t>(pmc.WorkingSetSize);
#endif
    return 0;
}

/**
 *	Return the process virtual memory.
 *
 *	@return	The process virtual memory in bytes.
 */
extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY ProcessVirtualMemory()
{
#if defined(__MACH__)
    mach_port_t task = mach_task_self();
	struct task_vm_info info;
	mach_msg_type_number_t size = TASK_VM_INFO_COUNT;
	if(KERN_SUCCESS == task_info(task, TASK_VM_INFO, (task_info_t)&info, &size))
    {
        return static_cast<int64_t>(info.virtual_size);
    }
#elif defined(__ANDROID__)
	static const char* const sums[] = { "VmSize:", NULL };
	static const size_t sumsLen[] = { strlen("VmSize:"), 0 };
	return getMemoryImpl(__FUNCTION__, "/proc/self/status", sums, sumsLen, 1) * KIBIBYTES_TO_BYTES;
#elif defined(_WIN32)
	PROCESS_MEMORY_COUNTERS_EX pmc;
	GetProcessMemoryInfo(GetCurrentProcess(), reinterpret_cast<PROCESS_MEMORY_COUNTERS*>(&pmc), sizeof(pmc));
	return static_cast<int64_t>(pmc.PrivateUsage);
#endif
    return 0;
}

/**
 *	Return the system free memory.
 *
 *	@return	The system free memory in bytes.
 */
extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY SystemFreeMemory()
{
#if defined(__MACH__)
    mach_port_t host = mach_host_self();
    mach_msg_type_number_t host_size = sizeof(vm_statistics_data_t) / sizeof(integer_t);
    vm_size_t pagesize;
    vm_statistics_data_t vm_stat;
    if(KERN_SUCCESS == host_page_size(host, &pagesize))
    {
        if(KERN_SUCCESS == host_statistics(host, HOST_VM_INFO, (host_info_t)&vm_stat, &host_size))
        {
            return static_cast<int64_t>(vm_stat.free_count * pagesize);
        }
    }
#elif defined(__ANDROID__)
    static const char* const sums[] = { "MemFree:", "Cached:", NULL };
    static const size_t sumsLen[] = { strlen("MemFree:"), strlen("Cached:"), 0 };
    return getMemoryImpl(__FUNCTION__, "/proc/meminfo", sums, sumsLen, 2) * KIBIBYTES_TO_BYTES;
#elif defined(_WIN32)
	MEMORYSTATUSEX memInfo;
	memInfo.dwLength = sizeof(MEMORYSTATUSEX);
	GlobalMemoryStatusEx(&memInfo);
	return static_cast<int64_t>(memInfo.ullTotalPhys - memInfo.ullAvailPhys);
#endif
    return 0;
}

/**
 *	Return the system total memory.
 *
 *	@return	The system total memory in bytes.
 */
extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY SystemTotalMemory()
{
#if defined(__MACH__)
    mach_port_t host = mach_host_self();
    mach_msg_type_number_t host_size = sizeof(vm_statistics_data_t) / sizeof(integer_t);
    vm_size_t pagesize;
    vm_statistics_data_t vm_stat;
    if(KERN_SUCCESS == host_page_size(host, &pagesize))
    {
        if(KERN_SUCCESS == host_statistics(host, HOST_VM_INFO, (host_info_t)&vm_stat, &host_size))
        {
			natural_t used_count = vm_stat.active_count + vm_stat.inactive_count + vm_stat.wire_count;
            return static_cast<int64_t>((vm_stat.free_count + used_count) * pagesize);
        }
    }
#elif defined(__ANDROID__)
    static const char* const sums[] = { "MemTotal:", NULL };
    static const size_t sumsLen[] = { strlen("MemTotal:"), 0 };
    return getMemoryImpl(__FUNCTION__, "/proc/meminfo", sums, sumsLen, 1) * KIBIBYTES_TO_BYTES;
#elif defined(_WIN32)
	MEMORYSTATUSEX memInfo;
	memInfo.dwLength = sizeof(MEMORYSTATUSEX);
	GlobalMemoryStatusEx(&memInfo);
	return static_cast<int64_t>(memInfo.ullTotalPhys);
#endif
    return 0;
}

/**
 *	Memory-map a file.
 *
 *	@param	path	Path to the file on-disk.
 *	@param[out]	data	The memory-mapped file address.
 *	@param[out]	size	The memory-mapped file size.
 */
extern "C"
PLUGIN_APICALL void* PLUGIN_APIENTRY MemoryMap(const char* path, void** data, int64_t* size)
{
	void* handle = NULL;
	*data = NULL;
	*size = -1;
	
#if defined(_WIN32)
	HANDLE fileHandle = ::CreateFile(path, GENERIC_READ, FILE_SHARE_READ, 0, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
	
	if(INVALID_HANDLE_VALUE != fileHandle)
	{
		DWORD sizeHI;
		DWORD sizeLO = ::GetFileSize(fileHandle, &sizeHI);
		size_t fileSize = (static_cast<size_t>(sizeHI) << 32) | static_cast<size_t>(sizeLO);
		
		if(0 != fileSize)
		{
			HANDLE mappingHandle = ::CreateFileMappingA(fileHandle, 0, PAGE_READONLY, sizeHI, sizeLO, NULL);
			
			if(INVALID_HANDLE_VALUE != mappingHandle)
			{
				*data = ::MapViewOfFile(mappingHandle, FILE_MAP_READ, 0, 0, fileSize);
				
				if(NULL == *data)
				{
					::CloseHandle(mappingHandle);
					*data = NULL;
				}
				else
				{
					handle = reinterpret_cast<void*>(mappingHandle);
					*size = static_cast<int64_t>(fileSize);
				}
			}
		}
		
		::CloseHandle(fileHandle);
	}
#else
	FILE* fd = ::fopen(path, "rb");
	
	if(NULL != fd)
	{
		struct stat stat;
		::fstat(fileno(fd), &stat);

		if(0 != stat.st_size)
		{
			*data = ::mmap(NULL, stat.st_size, PROT_READ, MAP_PRIVATE, fileno(fd), 0);
			
			if(MAP_FAILED == *data)
			{
				*data = NULL;
			}
			else
			{
				*size = stat.st_size;
			}
		}
		
		::fclose(fd);
	}
#endif
	
	return handle;
}

/**
 *	Unmap a memory-mapped file.
 *
 *	@param	handle	The memory-mapped file handle.
 *	@param	data	The memory-mapped file address.
 *	@param	size	The memory-mapped file size.
 */
extern "C"
PLUGIN_APICALL void PLUGIN_APIENTRY MemoryUnMap(void* handle, void* data, int64_t size)
{
#if defined(_WIN32)
	::UnmapViewOfFile(data);
	::CloseHandle(handle);
#else
	::munmap(data, size);
#endif
}
