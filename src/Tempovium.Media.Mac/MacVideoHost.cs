using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Tempovium.Media.Mac.Backends;

namespace Tempovium.Media.Mac;

public sealed class MacVideoHost : NativeControlHost
{
    public static readonly StyledProperty<MacMediaBackend?> BackendProperty =
        AvaloniaProperty.Register<MacVideoHost, MacMediaBackend?>(nameof(Backend));

    public MacMediaBackend? Backend
    {
        get => GetValue(BackendProperty);
        set => SetValue(BackendProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BackendProperty)
        {
            InvalidateVisual();
        }
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        if (Backend is null)
        {
            return base.CreateNativeControlCore(parent);
        }

        var viewHandle = Backend.GetNativeViewHandle();

        if (viewHandle == 0)
        {
            return base.CreateNativeControlCore(parent);
        }

        return new PlatformHandle(viewHandle, "NSView");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // El lifecycle lo maneja AVPlayerView.
    }
}