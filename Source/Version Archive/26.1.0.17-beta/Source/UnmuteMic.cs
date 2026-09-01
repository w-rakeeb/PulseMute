using System;
using System.Runtime.InteropServices;

internal static class UnmuteMic
{
    private static int Main()
    {
        bool changed = false;
        TryUnmute(ERole.Communications, ref changed);
        TryUnmute(ERole.Multimedia, ref changed);
        TryUnmute(ERole.Console, ref changed);
        Console.WriteLine(changed ? "Microphone mute was turned off." : "Microphone was already unmuted, or no default microphone was available.");
        return 0;
    }

    private static void TryUnmute(ERole role, ref bool changed)
    {
        IMMDeviceEnumerator enumerator = null;
        IMMDevice device = null;
        object endpointObject = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            if (enumerator.GetDefaultAudioEndpoint(EDataFlow.Capture, role, out device) < 0 || device == null)
            {
                Console.WriteLine(role + ": no default capture device");
                return;
            }

            Console.WriteLine(role + ": device=" + GetDeviceName(device));

            Guid iid = typeof(IAudioEndpointVolume).GUID;
            if (device.Activate(ref iid, 23, IntPtr.Zero, out endpointObject) < 0 || endpointObject == null)
            {
                Console.WriteLine(role + ": could not read capture device volume");
                return;
            }

            IAudioEndpointVolume endpoint = (IAudioEndpointVolume)endpointObject;
            bool muted;
            float volume;
            endpoint.GetMasterVolumeLevelScalar(out volume);
            if (endpoint.GetMute(out muted) >= 0)
            {
                Console.WriteLine(role + ": muted=" + muted + ", volume=" + Math.Round(volume * 100) + "%");
                if (muted)
                {
                    endpoint.SetMute(false, Guid.Empty);
                    changed = true;
                }
            }
        }
        catch
        {
        }
        finally
        {
            if (endpointObject != null && Marshal.IsComObject(endpointObject))
                Marshal.ReleaseComObject(endpointObject);
            if (device != null && Marshal.IsComObject(device))
                Marshal.ReleaseComObject(device);
            if (enumerator != null && Marshal.IsComObject(enumerator))
                Marshal.ReleaseComObject(enumerator);
        }
    }

    private static string GetDeviceName(IMMDevice device)
    {
        IntPtr storePtr = IntPtr.Zero;
        IPropertyStore store = null;
        try
        {
            if (device.OpenPropertyStore(0, out store) < 0 || store == null)
                return "unknown";

            PropertyKey key = new PropertyKey(new Guid(0xA45C254E, 0xDF1C, 0x4EFD, 0x80, 0x20, 0x67, 0xD1, 0x46, 0xA8, 0x50, 0xE0), 14);
            PropVariant value;
            if (store.GetValue(ref key, out value) >= 0 && !string.IsNullOrEmpty(value.Value))
                return value.Value;
        }
        catch
        {
        }
        finally
        {
            if (store != null && Marshal.IsComObject(store))
                Marshal.ReleaseComObject(store);
            if (storePtr != IntPtr.Zero)
                Marshal.Release(storePtr);
        }

        return "unknown";
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
    }

    private enum ERole
    {
        Console,
        Multimedia,
        Communications
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    private interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppDevice);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    private interface IMMDevice
    {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId(out IntPtr ppstrId);
        int GetState(out uint pdwState);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    private interface IPropertyStore
    {
        int GetCount(out uint cProps);
        int GetAt(uint iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant propvar);
        int Commit();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    private interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out uint pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        private readonly Guid formatId;
        private readonly int propertyId;

        public PropertyKey(Guid formatId, int propertyId)
        {
            this.formatId = formatId;
            this.propertyId = propertyId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        private readonly ushort vt;
        private readonly ushort reserved1;
        private readonly ushort reserved2;
        private readonly ushort reserved3;
        private readonly IntPtr pointer;
        private readonly int pointer2;

        public string Value
        {
            get { return vt == 31 && pointer != IntPtr.Zero ? Marshal.PtrToStringUni(pointer) : null; }
        }
    }
}
