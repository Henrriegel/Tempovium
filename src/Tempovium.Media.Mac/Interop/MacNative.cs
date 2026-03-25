using System.Runtime.InteropServices;

namespace Tempovium.Media.Mac.Interop;

internal static partial class MacNative
{
    private const string LibraryName = "TempoviumMacBridge";

    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_create")]
    internal static partial nint Create();

    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_destroy")]
    internal static partial void Destroy(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_load_file", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int LoadFile(nint handle, string path);

    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_play")]
    internal static partial void Play(nint handle);

    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_pause")]
    internal static partial void Pause(nint handle);
    
    [LibraryImport(LibraryName, EntryPoint = "tpv_mac_player_get_view")]
    internal static partial nint GetView(nint handle);
    
    [LibraryImport("TempoviumMacBridge")]
    public static partial void tpv_mac_player_get_state(
        IntPtr handle,
        out double position,
        out double duration
    );
}