using System;
using System.Runtime.InteropServices;

internal static class RecoverAllMics
{
    private const uint DeviceStateActive = 0x00000001;

    private static int Main()
    {
        IMMDeviceEnumerator enumerator = null;
        IMMDeviceCollection devices = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            enumerator.EnumAudioEndpoints(EDataFlow.Capture, DeviceStateActive, out devices).ThrowIfFailed();
            uint count;
            devices.GetCount(out count).ThrowIfFailed();
            Console.WriteLine("Active recording inputs: " + count);

            for (uint i = 0; i < count; i++)
            {
                IMMDevice device = null;
                object endpointObject = null;
                try
                {
                    devices.Item(i, out device).ThrowIfFailed();
                    string name = GetDeviceName(device);

                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    device.Activate(ref iid, 23, IntPtr.Zero, out endpointObject).ThrowIfFailed();
                    IAudioEndpointVolume endpoint = (IAudioEndpointVolume)endpointObject;

                    bool muted;
                    float volume;
                    endpoint.GetMute(out muted).ThrowIfFailed();
                    endpoint.GetMasterVolumeLevelScalar(out volume).ThrowIfFailed();

                    if (muted)
                        endpoint.SetMute(false, Guid.Empty).ThrowIfFailed();

                    Console.WriteLine(name + " | muted was " + muted + " | volume " + Math.Round(volume * 100) + "%");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not repair one input: " + ex.Message);
                }
                finally
                {
                    if (endpointObject != null && Marshal.IsComObject(endpointObject))
                        Marshal.ReleaseComObject(endpointObject);
                    if (device != null && Marshal.IsComObject(device))
                        Marshal.ReleaseComObject(device);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Recovery failed: " + ex.Message);
            return 1;
        }
        finally
        {
            if (devices != null && Marshal.IsComObject(devices))
                Marshal.ReleaseComObject(devices);
            if (enumerator != null && Marshal.IsComObject(enumerator))
                Marshal.ReleaseComObject(enumerator);
        }

        return 0;
    }

    private static string GetDeviceName(IMMDevice device)
    {
        IPropertyStore store = null;
        try
        {
            device.OpenPropertyStore(0, out store).ThrowIfFailed();
            PropertyKey key = new PropertyKey(new Guid(0xA45C254E, 0xDF1C, 0x4EFD, 0x80, 0x20, 0x67, 0xD1, 0x46, 0xA8, 0x50, 0xE0), 14);
            PropVariant value;
            store.GetValue(ref key, out value).ThrowIfFailed();
            return string.IsNullOrEmpty(value.Value) ? "Unknown recording input" : value.Value;
        }
        catch
        {
            return "Unknown recording input";
        }
        finally
        {
            if (store != null && Marshal.IsComObject(store))
                Marshal.ReleaseComObject(store);
        }
    }

    private static void ThrowIfFailed(this int hresult)
    {
        if (hresult < 0)
            Marshal.ThrowExceptionForHR(hresult);
    }

    private enum EDataFlow
    {
        Render,
        Capture,
        All
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
        int EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, int role, out IMMDevice ppDevice);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0BD7A1BE-7A1A-44DB-8397-C0DE6F44E008")]
    private interface IMMDeviceCollection
    {
        int GetCount(out uint pcDevices);
        int Item(uint nDevice, out IMMDevice ppDevice);
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
