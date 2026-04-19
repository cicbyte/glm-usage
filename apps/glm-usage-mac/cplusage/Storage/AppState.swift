import Foundation
import Combine

@MainActor
final class AppState: ObservableObject {
    static let shared = AppState()

    @Published var provider: (any Provider)?
    @Published var isRefreshing = false
    @Published var lastRefreshTime: Date?
    @Published var errorMessage: String?

    private var refreshTimer: Timer?
    private var lastNotifiedWarning = false
    private var lastNotifiedDanger = false

    private init() {}

    // MARK: - Setup

    func setupProvider() {
        let provider = GLMProvider()
        let apiKey = KeychainService.shared.load(key: Constants.Keychain.glmApiKey)
        if let apiKey, !apiKey.isEmpty {
            provider.configure(apiKey: apiKey, baseURL: nil)
        }
        self.provider = provider

        // Request notification authorization
        Task {
            _ = await NotificationService.shared.requestAuthorization()
        }

        if ConfigService.shared.autoRefreshOnLaunch, provider.isConfigured {
            Task { await refresh() }
        }
        startAutoRefresh()
    }

    // MARK: - API Key

    func saveAPIKey(_ key: String) {
        do {
            try KeychainService.shared.save(key: Constants.Keychain.glmApiKey, value: key)
            provider?.configure(apiKey: key, baseURL: nil)
            errorMessage = nil
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    func reconfigureAndRefresh() {
        let apiKey = KeychainService.shared.load(key: Constants.Keychain.glmApiKey)
        if let apiKey, !apiKey.isEmpty {
            provider?.configure(apiKey: apiKey, baseURL: nil)
            Task { await refresh() }
        }
    }

    func deleteAPIKey() {
        do {
            try KeychainService.shared.delete(key: Constants.Keychain.glmApiKey)
            provider = GLMProvider()
        } catch {
            errorMessage = error.localizedDescription
        }
    }

    // MARK: - Refresh

    func refresh() async {
        guard let provider, provider.isConfigured else { return }

        isRefreshing = true
        errorMessage = nil
        provider.isLoading = true

        do {
            let snapshot = try await provider.fetchUsage()
            provider.currentUsage = snapshot
            provider.lastError = nil
            lastRefreshTime = Date()

            // Save history
            let tokenPct = tokenPercentage
            HistoryManager.shared.addRecord(UsageRecord(
                timestamp: snapshot.timestamp,
                providerId: snapshot.providerId,
                planLevel: snapshot.planLevel,
                percentage: tokenPct
            ))

            // Check thresholds
            checkThresholds(percentage: tokenPct)
        } catch {
            provider.lastError = error.localizedDescription
            errorMessage = error.localizedDescription
        }

        isRefreshing = false
        provider.isLoading = false
    }

    func manualRefresh() async {
        await refresh()
    }

    // MARK: - Auto Refresh

    func startAutoRefresh() {
        stopAutoRefresh()
        let interval = ConfigService.shared.refreshInterval.rawValue
        refreshTimer = Timer.scheduledTimer(withTimeInterval: interval, repeats: true) { [weak self] _ in
            guard let self else { return }
            Task { @MainActor in
                await self.refresh()
            }
        }
    }

    func stopAutoRefresh() {
        refreshTimer?.invalidate()
        refreshTimer = nil
    }

    func restartAutoRefresh() {
        startAutoRefresh()
    }

    // MARK: - Thresholds

    private func checkThresholds(percentage: Double) {
        let config = ConfigService.shared
        guard config.notificationsEnabled else { return }

        if percentage >= Double(config.dangerThreshold) && !lastNotifiedDanger {
            NotificationService.shared.sendUsageDanger(percentage: percentage, provider: "GLM")
            lastNotifiedDanger = true
            lastNotifiedWarning = true
        } else if percentage >= Double(config.warningThreshold) && !lastNotifiedWarning {
            NotificationService.shared.sendUsageWarning(percentage: percentage, provider: "GLM")
            lastNotifiedWarning = true
        }

        // Reset notification flags when usage drops
        if percentage < Double(config.warningThreshold) {
            lastNotifiedWarning = false
            lastNotifiedDanger = false
        }
    }

    // MARK: - Computed

    /// Token 限额百分比
    var tokenPercentage: Double {
        provider?.currentUsage?.limits
            .first(where: { $0.type == .token })?
            .percentage ?? 0
    }

    /// Overall status for menu bar icon
    var status: AppStatus {
        guard let provider else { return .unconfigured }
        if provider.isLoading { return .loading }
        if provider.lastError != nil { return .error }
        guard provider.isConfigured else { return .unconfigured }
        let pct = tokenPercentage
        if pct >= Double(ConfigService.shared.dangerThreshold) { return .danger }
        if pct >= Double(ConfigService.shared.warningThreshold) { return .warning }
        return .normal
    }
}

enum AppStatus {
    case normal
    case warning
    case danger
    case loading
    case error
    case unconfigured
}
