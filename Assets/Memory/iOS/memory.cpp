#include <stdint.h>

#if defined(__MACH__)
#include <mach/mach.h>
#elif defined(__ANDROID__)
#include <fcntl.h>
#include <stdlib.h>
#include <android/log.h>
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
static int64_t getMemoryImpl(const char* const from, const char* const file, const char* const sums[], const size_t sumsLen[], size_t num)
{
	int fd = open(file, O_RDONLY | O_CLOEXEC);
	if(fd < 0)
	{
		__android_log_print(ANDROID_LOG_DEBUG, from, "Unable to open %s\n", file);
		return -1;
	}
	
	char buffer[512];
	const ssize_t len = read(fd, buffer, sizeof(buffer) - 1);
	close(fd);
	if(len < 0)
	{
		__android_log_print(ANDROID_LOG_DEBUG, from, "Unable to read %s\n", file);
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
				
				// Skip spaces.
				while(*p == ' ' || *p == '\t') ++p;
				
				// Number start.
				char* num = p;
				
				// Find the number end.
				while (*p >= '0' && *p <= '9') ++p;
				
				// If not at the buffer end.
				if(*p != 0)
				{
					// 0 terminated the number string.
					*p++ = 0;
					
					// Just make sure we are not at the buffer end.
					if(*p == 0) --p;
				}
				// Convert the number string (KB) to an actual number and add it to the total.
				mem += atoll(num) * 1024;
				
				break;
			}
		}
	}
	
	return numFound > 0 ? mem : -1;
}
#endif

extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY ProcessResidentMemory()
{
#if defined(__MACH__)
    mach_port_t task = mach_task_self();
    struct task_basic_info info;
    mach_msg_type_number_t size = sizeof(info);
    if(KERN_SUCCESS == task_info(task, TASK_BASIC_INFO, (task_info_t)&info, &size))
    {
        return static_cast<int64_t>(info.resident_size);
    }
#elif defined(__ANDROID__)
	static const char* const sums[] = { "VmRSS:", NULL };
	static const size_t sumsLen[] = { strlen("VmRSS:"), 0 };
	return getMemoryImpl("ProcessResidentMemory", "/proc/self/status", sums, sumsLen, 1);
#endif
    return 0;
}

extern "C"
PLUGIN_APICALL int64_t PLUGIN_APIENTRY ProcessVirtualMemory()
{
#if defined(__MACH__)
    mach_port_t task = mach_task_self();
    struct task_basic_info info;
    mach_msg_type_number_t size = sizeof(info);
    if(KERN_SUCCESS == task_info(task, TASK_BASIC_INFO, (task_info_t)&info, &size))
    {
        return static_cast<int64_t>(info.virtual_size);
    }
#elif defined(__ANDROID__)
	static const char* const sums[] = { "VmSize:", NULL };
	static const size_t sumsLen[] = { strlen("VmSize:"), 0 };
	return getMemoryImpl("ProcessVirtualMemory", "/proc/self/status", sums, sumsLen, 1);
#endif
    return 0;
}

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
    return getMemoryImpl("SystemFreeMemory", "/proc/meminfo", sums, sumsLen, 2);
#endif
    return 0;
}

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
    return getMemoryImpl("SystemTotalMemory", "/proc/meminfo", sums, sumsLen, 1);
#endif
    return 0;
}
