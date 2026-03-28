using System;
using System.Runtime.InteropServices;

namespace Tempovium.Media.Mac.Interop;

internal static class MacNative
{
    private const string LibraryName = "TempoviumMacBridge";

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_create")]
    public static extern nint Create();

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_destroy")]
    public static extern void Destroy(nint handle);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_load_file", CharSet = CharSet.Ansi)]
    public static extern int LoadFile(nint handle, string path);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_play")]
    public static extern void Play(nint handle);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_pause")]
    public static extern void Pause(nint handle);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_get_view")]
    public static extern nint GetView(nint handle);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_get_state")]
    public static extern void tpv_mac_player_get_state(
        nint handle,
        out double position,
        out double duration,
        out int isReady);

    [DllImport(LibraryName, EntryPoint = "tpv_mac_player_seek")]
    public static extern void tpv_mac_player_seek(nint handle, double seconds);
}