import Foundation
import AppKit
import AVKit
import AVFoundation

final class PlayerContainer: NSObject {
    let player: AVPlayer
    let playerView: AVPlayerView

    private var statusObservation: NSKeyValueObservation?
    private var isReadyToPlay = false

    private var pendingSeekSeconds: Double?
    private let seekSettleToleranceSeconds: Double = 0.35

    @MainActor
    override init() {
        player = AVPlayer()
        playerView = AVPlayerView()

        super.init()

        playerView.player = player
        playerView.controlsStyle = .inline

        player.defaultRate = 1.0
        player.rate = 0.0
    }

    @MainActor
    func load(url: URL) {
        if !FileManager.default.fileExists(atPath: url.path) {
            print("❌ Archivo no existe:", url.path)
            return
        }

        print("📂 Cargando archivo:", url.path)

        statusObservation?.invalidate()
        statusObservation = nil
        isReadyToPlay = false
        pendingSeekSeconds = nil

        player.pause()
        player.rate = 0.0
        player.replaceCurrentItem(with: nil)

        let item = AVPlayerItem(url: url)
        let selfRawValue = UInt(bitPattern: Unmanaged.passUnretained(self).toOpaque())

        statusObservation = item.observe(\.status, options: [.initial, .new]) { observedItem, _ in
            DispatchQueue.main.async {
                guard let pointer = UnsafeMutableRawPointer(bitPattern: selfRawValue) else { return }

                let owner = Unmanaged<PlayerContainer>
                    .fromOpaque(pointer)
                    .takeUnretainedValue()

                switch observedItem.status {
                case .readyToPlay:
                    owner.isReadyToPlay = true
                    print("✅ Item listo para reproducir")
                case .failed:
                    owner.isReadyToPlay = false
                    print("❌ Item falló al cargar:", observedItem.error?.localizedDescription ?? "sin detalle")
                case .unknown:
                    owner.isReadyToPlay = false
                    print("⏳ Item aún no listo")
                @unknown default:
                    owner.isReadyToPlay = false
                    print("⚠️ Estado desconocido del item")
                }
            }
        }

        player.replaceCurrentItem(with: item)

        player.pause()
        player.rate = 0.0

        player.seek(to: .zero) { _ in
            DispatchQueue.main.async {
                guard let pointer = UnsafeMutableRawPointer(bitPattern: selfRawValue) else { return }

                let owner = Unmanaged<PlayerContainer>
                    .fromOpaque(pointer)
                    .takeUnretainedValue()

                owner.player.pause()
                owner.player.rate = 0.0
                owner.pendingSeekSeconds = nil

                print("⏹ rate después de cargar:", owner.player.rate)
                print("⏹ time después de cargar:", owner.player.currentTime().seconds)
            }
        }
    }

    @MainActor
    func play() {
        print("▶️ Swift play()")

        player.defaultRate = 1.0
        player.playImmediately(atRate: 1.0)
        player.rate = 1.0

        print("▶️ rate tras play():", player.rate)
    }

    @MainActor
    func pause() {
        print("⏸ Swift pause()")
        player.pause()
        player.rate = 0.0
        print("⏸ rate tras pause():", player.rate)
    }

    @MainActor
    func getState() -> (Double, Double, Int32) {
        let currentRate = player.rate
        if currentRate > 1.01 {
            print("⚠️ corrigiendo rate anómalo:", currentRate)
            player.rate = 1.0
            player.defaultRate = 1.0
        }

        let currentTime = player.currentTime().seconds
        let totalTime = player.currentItem?.duration.seconds ?? 0

        let safePosition = currentTime.isFinite ? currentTime : 0
        let safeDuration = totalTime.isFinite ? totalTime : 0

        var effectivePosition = safePosition

        if let target = pendingSeekSeconds {
            if abs(safePosition - target) <= seekSettleToleranceSeconds {
                pendingSeekSeconds = nil
                effectivePosition = safePosition
            } else {
                effectivePosition = target
            }
        }

        let ready = isReadyToPlay ? Int32(1) : Int32(0)
        print("[SwiftBridge] getState -> current=\(safePosition), effective=\(effectivePosition), duration=\(safeDuration), pending=\(String(describing: pendingSeekSeconds)), ready=\(isReadyToPlay)")
        return (effectivePosition, safeDuration, ready)
    }

    @MainActor
    func seek(to seconds: Double) {
        let clampedSeconds = max(0, seconds)
        pendingSeekSeconds = clampedSeconds
        print("[SwiftBridge] seek -> requested=\(seconds), clamped=\(clampedSeconds)")

        let time = CMTime(seconds: clampedSeconds, preferredTimescale: 600)
        let selfRawValue = UInt(bitPattern: Unmanaged.passUnretained(self).toOpaque())

        player.seek(
            to: time,
            toleranceBefore: .zero,
            toleranceAfter: .zero
        ) { finished in
            DispatchQueue.main.async {
                guard let pointer = UnsafeMutableRawPointer(bitPattern: selfRawValue) else { return }

                let owner = Unmanaged<PlayerContainer>
                    .fromOpaque(pointer)
                    .takeUnretainedValue()

                guard finished else { return }

                let actual = owner.player.currentTime().seconds
                let safeActual = actual.isFinite ? actual : 0

                print("[SwiftBridge] seek completion -> finished=\(finished), actual=\(safeActual), pending=\(String(describing: owner.pendingSeekSeconds))")
                if let target = owner.pendingSeekSeconds,
                   abs(safeActual - target) <= owner.seekSettleToleranceSeconds {
                    owner.pendingSeekSeconds = nil
                }
            }
        }
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
    _ duration: UnsafeMutablePointer<Double>?,
    _ isReady: UnsafeMutablePointer<Int32>?
) {
    guard let handle else {
        position?.pointee = 0
        duration?.pointee = 0
        isReady?.pointee = 0
        return
    }

    let rawValue = UInt(bitPattern: handle)

    let state: (Double, Double, Int32) = runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else {
            return (0, 0, 0)
        }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        return obj.getState()
    }

    position?.pointee = state.0
    duration?.pointee = state.1
    isReady?.pointee = state.2
}

@_cdecl("tpv_mac_player_seek")
public func tpv_mac_player_seek(
    _ handle: UnsafeMutableRawPointer?,
    _ seconds: Double
) {
    guard let handle else { return }

    let rawValue = UInt(bitPattern: handle)

    runOnMainActorSync {
        guard let pointer = UnsafeMutableRawPointer(bitPattern: rawValue) else { return }

        let obj = Unmanaged<PlayerContainer>.fromOpaque(pointer).takeUnretainedValue()
        obj.seek(to: seconds)
    }
}
