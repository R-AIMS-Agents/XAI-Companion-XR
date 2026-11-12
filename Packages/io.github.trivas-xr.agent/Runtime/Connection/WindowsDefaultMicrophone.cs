using System;
using System.Runtime.InteropServices;

namespace TrIVA.Connection
{
    /// <summary>
    /// Resolves the friendly name of the Windows default recording device via the Core Audio API, since Unity's Microphone API cannot tell which device is the system default.
    /// </summary>
    public static class WindowsDefaultMicrophone
    {
        /// <summary>
        /// Returns the default recording device's friendly name, or null when not on Windows, no device exists or the lookup fails.
        /// </summary>
        public static string GetName()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            IPropertyStore store = null;
            PropVariant value = default;
            try
            {
                enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                enumerator.GetDefaultAudioEndpoint(DataFlowCapture, RoleConsole, out device);
                device.OpenPropertyStore(StgmRead, out store);
                PropertyKey key = FriendlyNameKey;
                store.GetValue(ref key, out value);
                return value.Type == VtLpwstr ? Marshal.PtrToStringUni(value.Pointer) : null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                PropVariantClear(ref value);
                if (store != null)
                    Marshal.ReleaseComObject(store);
                if (device != null)
                    Marshal.ReleaseComObject(device);
                if (enumerator != null)
                    Marshal.ReleaseComObject(enumerator);
            }
#else
            return null;
#endif
        }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        private const int DataFlowCapture = 1;
        private const int RoleConsole = 0;
        private const int StgmRead = 0;
        private const ushort VtLpwstr = 31;

        private static readonly PropertyKey FriendlyNameKey = new()
        {
            FormatId = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
            PropertyId = 14
        };

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);

        /// <summary>
        /// COM class of the Windows multimedia device enumerator.
        /// </summary>
        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        /// <summary>
        /// Enumerates audio endpoints; only the members up to the default endpoint lookup are declared.
        /// </summary>
        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            void EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            void GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        }

        /// <summary>
        /// A single audio endpoint; only the members up to the property store access are declared.
        /// </summary>
        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            void Activate(ref Guid interfaceId, int classContext, IntPtr activationParams, out IntPtr instance);
            void OpenPropertyStore(int access, out IPropertyStore store);
        }

        /// <summary>
        /// Read access to an endpoint's properties; only the members up to value lookup are declared.
        /// </summary>
        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out int count);
            void GetAt(int index, out PropertyKey key);
            void GetValue(ref PropertyKey key, out PropVariant value);
        }

        /// <summary>
        /// Identifies a device property by format GUID and property ID.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid FormatId;
            public int PropertyId;
        }

        /// <summary>
        /// Minimal PROPVARIANT layout sized for 64-bit, exposing the type tag and the string pointer.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort Type;
            [FieldOffset(8)] public IntPtr Pointer;
        }
#endif
    }
}
