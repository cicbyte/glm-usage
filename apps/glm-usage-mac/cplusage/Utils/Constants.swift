import Foundation

enum Constants {
    enum App {
        static let name = "CPlusage"
        static let version = "1.0.0"
        static let build = 1
    }

    enum Keychain {
        static let service = "com.cplusage"
        static let glmApiKey = "com.cplusage.glm.apikey"
    }

    enum Defaults {
        static let refreshInterval = "refreshInterval"
        static let serverEndpoint = "serverEndpoint"
        static let autoRefreshOnLaunch = "autoRefreshOnLaunch"
        static let warningThreshold = "warningThreshold"
        static let dangerThreshold = "dangerThreshold"
        static let notificationsEnabled = "notificationsEnabled"
    }

    enum RefreshInterval: Double, CaseIterable, Identifiable {
        case oneMinute = 60
        case fiveMinutes = 300
        case fifteenMinutes = 900
        case thirtyMinutes = 1800

        var id: Double { rawValue }

        var label: String {
            switch self {
            case .oneMinute: return "1 分钟"
            case .fiveMinutes: return "5 分钟"
            case .fifteenMinutes: return "15 分钟"
            case .thirtyMinutes: return "30 分钟"
            }
        }
    }

    enum GLMServer: String, CaseIterable, Identifiable {
        case bigmodel = "https://open.bigmodel.cn"
        case zai = "https://api.z.ai"

        var id: String { rawValue }

        var label: String {
            switch self {
            case .bigmodel: return "open.bigmodel.cn"
            case .zai: return "api.z.ai"
            }
        }
    }
}
