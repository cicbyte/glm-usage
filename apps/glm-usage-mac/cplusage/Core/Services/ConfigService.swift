import Foundation
import Combine

final class ConfigService: ObservableObject {
    static let shared = ConfigService()

    @Published var refreshInterval: Constants.RefreshInterval = .fiveMinutes
    @Published var serverEndpoint: Constants.GLMServer = .bigmodel
    @Published var autoRefreshOnLaunch: Bool = true
    @Published var warningThreshold: Int = 80
    @Published var dangerThreshold: Int = 90
    @Published var notificationsEnabled: Bool = true

    private let defaults = UserDefaults.standard

    private init() {
        loadSettings()
    }

    func loadSettings() {
        if let rawInterval = defaults.object(forKey: Constants.Defaults.refreshInterval) as? Double,
           let interval = Constants.RefreshInterval(rawValue: rawInterval) {
            refreshInterval = interval
        }

        if let rawServer = defaults.string(forKey: Constants.Defaults.serverEndpoint),
           let server = Constants.GLMServer(rawValue: rawServer) {
            serverEndpoint = server
        }

        autoRefreshOnLaunch = defaults.object(forKey: Constants.Defaults.autoRefreshOnLaunch) as? Bool ?? true
        warningThreshold = defaults.object(forKey: Constants.Defaults.warningThreshold) as? Int ?? 80
        dangerThreshold = defaults.object(forKey: Constants.Defaults.dangerThreshold) as? Int ?? 90
        notificationsEnabled = defaults.object(forKey: Constants.Defaults.notificationsEnabled) as? Bool ?? true
    }

    func saveRefreshInterval(_ interval: Constants.RefreshInterval) {
        refreshInterval = interval
        defaults.set(interval.rawValue, forKey: Constants.Defaults.refreshInterval)
    }

    func saveServerEndpoint(_ server: Constants.GLMServer) {
        serverEndpoint = server
        defaults.set(server.rawValue, forKey: Constants.Defaults.serverEndpoint)
    }

    func saveAutoRefreshOnLaunch(_ value: Bool) {
        autoRefreshOnLaunch = value
        defaults.set(value, forKey: Constants.Defaults.autoRefreshOnLaunch)
    }

    func saveWarningThreshold(_ value: Int) {
        warningThreshold = value
        defaults.set(value, forKey: Constants.Defaults.warningThreshold)
    }

    func saveDangerThreshold(_ value: Int) {
        dangerThreshold = value
        defaults.set(value, forKey: Constants.Defaults.dangerThreshold)
    }

    func saveNotificationsEnabled(_ value: Bool) {
        notificationsEnabled = value
        defaults.set(value, forKey: Constants.Defaults.notificationsEnabled)
    }
}
