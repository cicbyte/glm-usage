import SwiftUI
import AppKit

@main
struct cplusageApp: App {
    @StateObject private var appState = AppState.shared

    var body: some Scene {
        MenuBarExtra {
            ContentView()
        } label: {
            menuBarLabel
        }
        .menuBarExtraStyle(.window)
    }

    @ViewBuilder
    private var menuBarLabel: some View {
        switch appState.status {
        case .normal, .warning, .danger:
            Text("GLM \(Formatters.percentage(appState.tokenPercentage))")
        case .loading:
            Image(systemName: "arrow.2.circlepath")
        case .error:
            Image(systemName: "xmark.octagon.fill")
        case .unconfigured:
            Image(systemName: "gear.badge.questionmark")
        }
    }

    init() {
        AppState.shared.setupProvider()
    }
}

// MARK: - Settings Window Helper

enum SettingsWindowHelper {
    private static weak var settingsWindow: NSWindow?

    static func open() {
        // 延迟执行，等 MenuBarExtra 弹出面板关闭
        DispatchQueue.main.async {
            NSApp.activate(ignoringOtherApps: true)

            if let window = settingsWindow, window.isVisible {
                window.makeKeyAndOrderFront(nil)
                return
            }

            let settingsView = SettingsView()
            let window = NSWindow(
                contentRect: NSRect(x: 0, y: 0, width: 400, height: 480),
                styleMask: [.titled, .closable],
                backing: .buffered,
                defer: false
            )
            window.title = "CPlusage 设置"
            window.isReleasedWhenClosed = false
            window.contentView = NSHostingView(rootView: settingsView)
            window.center()
            window.makeKeyAndOrderFront(nil)

            settingsWindow = window
        }
    }

    static func quit() {
        NSApp.terminate(nil)
    }
}
