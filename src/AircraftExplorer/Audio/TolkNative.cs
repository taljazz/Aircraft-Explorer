using System.Runtime.InteropServices;

namespace AircraftExplorer.Audio;

internal static class TolkNative
{
    private const string DllName = "Tolk.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Tolk_Load();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Tolk_Unload();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Tolk_TrySAPI(bool useSAPI);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    internal static extern bool Tolk_Output(string str, bool interrupt);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Tolk_Silence();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Tolk_IsLoaded();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr Tolk_DetectScreenReader();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern bool Tolk_HasSpeech();
}
