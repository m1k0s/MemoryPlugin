LOCAL_PATH := $(call my-dir)/../../../Assets/Memory/iOS

include $(CLEAR_VARS)
LOCAL_C_INCLUDES += .

LOCAL_CFLAGS += -fvisibility=hidden
# https://developer.android.com/guide/practices/page-sizes#compile-r26-lower
LOCAL_LDFLAGS += -Wl,-z,max-page-size=16384

LOCAL_ARM_MODE := arm
LOCAL_MODULE := memory
LOCAL_SRC_FILES := memory.cpp
LOCAL_LDLIBS := -llog
include $(BUILD_SHARED_LIBRARY)
