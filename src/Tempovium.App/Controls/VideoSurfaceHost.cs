using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Tempovium.Controls;

public class VideoSurfaceHost : NativeControlHost
{
    public event EventHandler<VideoSurfaceHandleReadyEventArgs>? HandleReady;

    public IPlatformHandle? NativeControlHandle { get; private set; }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        var handle = base.CreateNativeControlCore(parent);

        NativeControlHandle = handle;
        HandleReady?.Invoke(this, new VideoSurfaceHandleReadyEventArgs(handle));

        return handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        if (ReferenceEquals(NativeControlHandle, control))
        {
            NativeControlHandle = null;
        }

        base.DestroyNativeControlCore(control);
    }
}

public sealed class VideoSurfaceHandleReadyEventArgs : EventArgs
{
    public IPlatformHandle Handle { get; }

    public IntPtr HandlePointer => Handle.Handle;

    public string HandleDescriptor => Handle.HandleDescriptor;

    public VideoSurfaceHandleReadyEventArgs(IPlatformHandle handle)
    {
        Handle = handle;
    }
}