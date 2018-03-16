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
#endif
    return 0;
}

#if defined(__ANDROID__)
static int64_t getFreeMemoryImpl(const char* const from, const char* const sums[], const size_t sumsLen[], size_t num)
{
    int fd = open("/proc/meminfo", O_RDONLY | O_CLOEXEC);
    if(fd < 0)
    {
        __android_log_print(ANDROID_LOG_DEBUG, from, "Unable to open /proc/meminfo\n");
        return -1;
    }

    char buffer[256];
    const int len = read(fd, buffer, sizeof(buffer) - 1);
    close(fd);
    if(len < 0)
    {
        __android_log_print(ANDROID_LOG_DEBUG, from, "Unable to read /proc/meminfo\n");
        return -1;
    }

    // 0 terminate the buffer we just read.
    buffer[len] = 0;

    int numFound = 0;
    int64_t mem = 0;
    for(char* p = buffer; *p && numFound < num; ++p)
    {
        for(int i = 0; sums[i]; ++i)
        {
            if (strncmp(p, sums[i], sumsLen[i]) == 0)
            {
                p += sumsLen[i];

                while(*p == ' ') ++p;

                char* num = p;

                while (*p >= '0' && *p <= '9') ++p;

                if(*p != 0)
                {
                    *p++ = 0;

                    if(*p == 0) --p;
                }

                mem += atoll(num) * 1024;
                numFound++;

                break;
            }
        }
    }

    return numFound > 0 ? mem : -1;
}
#endif

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
    return getFreeMemoryImpl("SystemFreeMemory", sums, sumsLen, 2);
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
    return getFreeMemoryImpl("SystemTotalMemory", sums, sumsLen, 1);
#endif
    return 0;
}
