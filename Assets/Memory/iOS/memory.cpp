#ifdef __MACH__
#include <mach/mach.h>
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
PLUGIN_APICALL int64_t ProcessResidentMemory()
{
#ifdef __MACH__
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
PLUGIN_APICALL int64_t ProcessVirtualMemory()
{
#ifdef __MACH__
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

extern "C"
PLUGIN_APICALL int64_t SystemFreeMemory()
{
#ifdef __MACH__
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
#endif
    return 0;
}
