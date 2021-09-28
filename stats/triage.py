class ClassifyError(Exception):
    def __init__(self, msg):
        self.msg = msg

class Classification:
    def hash_stack(self, stack):
        return " || ".join(stack.split("\n"))

    def __init__(self, stack):
        self.stack_invalid = False
        self.hash = self.hash_stack(stack)
        # 1903/2004 latest
        if "PspCatchCriticalBreak" in stack and "ntdll" in stack:
            self.main_type = "Critical process termination from ntdll"
            self.sub_type = None
        elif "WARPKMADAPTER::StartGPU" in stack:
            self.main_type = "UAF in BasicRender"
            self.sub_type = "StartGPU"
        elif "WARPKMADAPTER::RunGPU" in stack:
            self.main_type = "UAF in BasicRender"
            self.sub_type = "RunGPU"
        elif "WARPKMDMABUFINFO::Discard" in stack and "DdiCancelCommand" in stack:
            self.main_type = "UAF in BasicRender"
            self.sub_type = "Discard"
        elif "VidSchiSubmitSignalCommand" in stack:
            self.main_type = "UAF in BasicRender"
            self.sub_type = "SyncSignal"
        elif "dxgmms2!VIDMM_MEMORY_SEGMENT::Init" in stack:
            self.main_type = "UAF in BasicRender"
            self.sub_type = "Stack corrupted"
            self.stack_invalid = True
        elif "SignalPresentLimitSemaphore" in stack or \
             "DxgkSignalSynchronizationObjectFromGpuByReference" in stack:
            self.main_type = "NULL deref in semaphore"
            self.sub_type = None
        elif "InitializeMiniWinInfo" in stack or "win32kfull!Ordinal" in stack:
            self.main_type = "Div-by-zero @ InitializeMiniWinInfo"
            self.sub_type = None
        elif "InputAABFDATAToAA24" in stack:
            self.main_type = "Invalid access @ NtGdiStretchDIBitsInternal"
            self.sub_type = None
        elif "EtwpTraceMessageVa" in stack and "NtTraceEvent" in stack:
            self.main_type = "Invalid access @ NtTraceEvent"
            self.sub_type = None
        elif "cdd" in stack and "NtGdiDrawStream" in stack:
            self.main_type = "Invalid access @ NtGdiDrawStream"
            self.sub_type = None
        elif "CanForceForeground" in stack:
            self.main_type = "NULL deref @ ForceForeground of csrss.exe"
            self.sub_type = None
        elif "NsiGetAllParametersEx" in stack:
            self.main_type = "NULL deref @ tcpip"
            self.sub_type = None
        elif "xxxCreateWindowEx" in stack:
            self.main_type = "Div-by-zero @ CreateWindowEx"
            self.sub_type = None
        elif stack.count("xxxDestroyWindow") >= 10:
            self.main_type = "Stack overflow @ DestroyWindow"
            self.sub_type = None
        elif "CitpInteractionSummaryStopTracking" in stack:
            self.main_type = "FSTP exception in win32kfull"
            self.sub_type = None
        # 1903 2020-01
        elif "AuthzBasep" in stack:
            self.main_type = "Uninitialized @ AuthzBasep*"
            if "NtOpenKeyEx" in stack:
                self.sub_type = "NtOpenKeyEx"
            elif "NtOpenKey" in stack:
                self.sub_type = "NtOpenKey"
            else:
                raise ClassifyError(self.main_type)
        elif "NtUserRegisterWindowMessage" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserRegisterWindowMessage"
        elif "NtUserRegisterClassExWOW" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserRegisterClassExWOW"
        elif "_RegisterClassEx" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserRegisterClassEx"
        elif "NtUserFindExistingCursorIcon" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserFindExistingCursorIcon"
        elif "NtUserSetCursorIconData" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserSetCursorIconData"
        elif "NtUserGetClassInfoEx" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserGetClassInfoEx"
        elif "NtUserThunkedMenuItemInfo" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserThunkedMenuItemInfo"
        elif "NtUserUnregisterClass" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserUnregisterClass"
        elif "NtUserSetWindowsHookEx" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserSetWindowsHookEx"
        elif "NtUserSetWinEventHook" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserSetWinEventHook"
        elif "NtUserFindWindowEx" in stack:
            self.main_type = "Telemetry"
            self.sub_type = "NtUserFindWindowEx"
        # 1803 2018-04
        elif "NtTraceControl" in stack:
            self.main_type = "NULL deref @ NtTraceControl"
            if "ObfReferenceObject" in stack:
                self.sub_type =  "ObfReferenceObject"
            elif "EtwpSetProviderBinaryTracking" in stack:
                self.sub_type = "EtwpSetProviderBinaryTracking"
            else:
                raise ClassifyError(self.main_type)
        elif "NtDeleteValueKey" in stack:
            self.main_type = "NULL deref @ NtDeleteValueKey"
            self.sub_type = None
        elif stack.startswith("nt!Kei386EoiHelper"):
            self.main_type = "General protection fault"
            self.sub_type = None
        elif "DrvFillPath" in stack:
            self.main_type = "Allocation error @ DrvFillPath"
            self.sub_type = None
        elif "NtCreateFile" in stack:
            self.main_type = "Allocation error @ NtCreateFile"
            self.sub_type = None
        elif "ExFreePoolWithTag" in stack and "NtQueryKey" in stack:
            self.main_type = "Free error @ NtQueryKey"
            self.sub_type = None
        elif "RPCRT4" in stack:
            self.main_type = "Critical process termination from RPCRT4"
            self.sub_type = None
        elif "NtUserSetMenu" in stack:
            self.main_type = "Div-by-zero in NtUserSetMenu"
            self.sub_type = None
        elif "NtUserEndDeferWindowPosEx" in stack:
            self.main_type = "Div-by-zero in NtUserSetMenu"
            self.sub_type = None
        elif "NtQueryLicenseValue" in stack:
            self.main_type = "NULL deref @ NtQueryLicenseValue"
            self.sub_type = None
        elif "NtUserShowScrollBar" in stack:
            self.main_type = "Div-by-zero @ NtUserShowScrollBar"
            self.sub_type = None
        elif "NtUserSetScrollBar" in stack:
            self.main_type = "Div-by-zero @ NtUserSetScrollBar"
            self.sub_type = None
        elif "NtUserSetScrollInfo" in stack:
            self.main_type = "Div-by-zero @ NtUserSetScrollInfo"
            self.sub_type = None
        elif "ObCloseHandleTableEntry" in stack:
            self.main_type = "NULL deref @ ObCloseHandleTableEntry"
            self.sub_type = None
        # ioctlfuzz
        elif "HvpCopyDataToOffsetArray" in stack:
            self.main_type = "memcpy error @ SystemThreadStartup"
            self.sub_type = None
        elif "ExpInterlockedPopEntrySListFault" in stack and \
             "NtCreateSection" in stack:
            self.main_type = "ExpInterlockedPopEntrySListFault"
            self.sub_type = None
        elif "RtlpHeapHandleError" in stack and \
             "NtQuerySystemInformation" in stack:
            self.main_type = "Heap error @ NtQuerySystemInformation"
            self.sub_type = None
        # ntcall
        elif "PnpBugcheckPowerTimeout" in stack:
            self.main_type = "PowerTimeout"
            self.sub_type = None
        elif "win32kbase!RGNOBJ::vSet" in stack:
            self.main_type = "RGNOBJ::vSet"
            self.sub_type = None
        elif "PnpSurpriseRemoveLockedDeviceNode" in stack:
            self.main_type = "SurpriseRemoveLockedDeviceNode"
            self.sub_type = None
        elif "PoBroadcastSystemState" in stack:
            self.main_type = "BroadcastSystemState"
            self.sub_type = None
        else:
            raise ClassifyError("No main type")
