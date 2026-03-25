import Foundation
import AppKit
import AVKit
import AVFoundation

@MainActor
final class PlayerContainer: NSObject {
    let player = AVPlayer()
    let playerView = AVPlayerView()

    override init() {
        super.init()
        playerView.player = player
        playerView.controlsStyle = .floating
    }

    func load(url: URL) {
        if !FileManager.default.fileExists(atPath: url.path) {
            print("❌ Archivo no existe:", url.path)
            return
        }

        let item = AVPlayerItem(url: url)
        player.replaceCurrentItem(with: item)
        player.play()
    }

    func play() {
        player.play()
    }

    func pause() {
        player.pause()
    }
}

private func runOnMainActorSync<T: Sendable>(_ work: @MainActor () -> T) -> T {
    if Thread.isMainThread {
        return MainActor.assumeIsolated {
            work()
        }
    }

    return DispatchQueue.main.sync {
        MainActor.assumeIsolated {
            work()
        }
    }
}

@_cdecl("tpv_mac_player_create")
public func tpv_mac_player_create() -> UnsafeMutableRawPointer? {
    let rawValue: UInt = runOnMainActorSync {
        let obj = PlayerContainer()
        let pointer = Unmanaged.passRetained(obj).toOpaque()
        return UInt(bitPattern: pointer)
    }

    return UnsafeMutableRawPointer(bitPattern: rawValue)
}

@_cdecl("tpv_mac_player_destroy")
public func tpv_mac_player_destroy(_ handle: UnsafeMutableRawPointer?) {
    guard let handle else { return }

    let rawValue = UInt(bitPattern: handle)

    runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return }
        Unmanaged<PlayerContainer>.fromOpaque(pointer).release()
    }
}

@_cdecl("tpv_mac_player_load_file")
public func tpv_mac_player_load_file(
    _ handle: UnsafeMutableRawPointer?,
    _ path: UnsafePointer<CChar>?
) -> Int32 {
    guard let handle, let path else { return 0 }

    let rawValue = UInt(bitPattern: handle)
    let stringPath = String(cString: path)

    print("📂 Cargando archivo:", stringPath)

    return runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return 0 }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        let url = URL(fileURLWithPath: stringPath)

        obj.load(url: url)
        return 1
    }
}

@_cdecl("tpv_mac_player_play")
public func tpv_mac_player_play(_ handle: UnsafeMutableRawPointer?) {
    guard let handle else { return }

    let rawValue = UInt(bitPattern: handle)

    runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        obj.play()
    }
}

@_cdecl("tpv_mac_player_pause")
public func tpv_mac_player_pause(_ handle: UnsafeMutableRawPointer?) {
    guard let handle else { return }

    let rawValue = UInt(bitPattern: handle)

    runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        obj.pause()
    }
}

@_cdecl("tpv_mac_player_get_view")
public func tpv_mac_player_get_view(_ handle: UnsafeMutableRawPointer?) -> UnsafeMutableRawPointer? {
    guard let handle else { return nil }

    let rawValue = UInt(bitPattern: handle)

    let viewRawValue: UInt = runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return 0 }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        let viewPointer = Unmanaged.passUnretained(obj.playerView).toOpaque()

        return UInt(bitPattern: viewPointer)
    }

    guard viewRawValue != 0 else { return nil }
    return UnsafeMutableRawPointer(bitPattern: viewRawValue)
}

@_cdecl("tpv_mac_player_get_state")
public func tpv_mac_player_get_state(
    _ handle: UnsafeMutableRawPointer?,
    _ position: UnsafeMutablePointer<Double>?,
    _ duration: UnsafeMutablePointer<Double>?
) {
    guard let handle else {
        position?.pointee = 0
        duration?.pointee = 0
        return
    }

    let rawValue = UInt(bitPattern: handle)

    let state: (Double, Double) = runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else {
            return (0, 0)
        }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()

        let currentTime = obj.player.currentTime().seconds
        let totalTime = obj.player.currentItem?.duration.seconds ?? 0

        let safePosition = currentTime.isFinite ? currentTime : 0
        let safeDuration = totalTime.isFinite ? totalTime : 0

        return (safePosition, safeDuration)
    }

    position?.pointee = state.0
    duration?.pointee = state.1
}
