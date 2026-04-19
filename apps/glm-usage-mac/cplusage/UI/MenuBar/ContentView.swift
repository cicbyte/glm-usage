import SwiftUI

struct ContentView: View {
    @ObservedObject var appState = AppState.shared
    @ObservedObject var config = ConfigService.shared

    var body: some View {
        VStack(spacing: 0) {
            header
            Divider()
            contentBody
            Divider()
            footer
        }
        .frame(width: 320)
    }

    // MARK: - Header

    private var header: some View {
        HStack {
            Image(systemName: "cpu")
                .foregroundStyle(.blue)
            Text("GLM")
                .font(.headline)
            if let usage = appState.provider?.currentUsage {
                Text(usage.planLevel.uppercased())
                    .font(.caption2.bold())
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(Color.accentColor.opacity(0.15))
                    .clipShape(Capsule())
            }
            Spacer()
            if appState.provider?.currentUsage != nil {
                Text(Formatters.percentage(appState.tokenPercentage))
                    .font(.title3.monospacedDigit().bold())
                    .foregroundStyle(colorForStatus(appState.status))
            }
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 12)
    }

    // MARK: - Body

    @ViewBuilder
    private var contentBody: some View {
        if let provider = appState.provider, provider.isConfigured {
            if let usage = provider.currentUsage {
                configuredContent(usage: usage)
            } else if appState.isRefreshing {
                loadingView
            } else if let error = provider.lastError {
                errorView(message: error)
            } else {
                placeholderView
            }
        } else {
            unconfiguredView
        }
    }

    private func configuredContent(usage: UsageSnapshot) -> some View {
        let sorted = usage.limits.sorted { lhs, rhs in
            let leftPriority = lhs.type == .token ? 0 : 1
            let rightPriority = rhs.type == .token ? 0 : 1
            return leftPriority < rightPriority
        }
        return ScrollView {
            VStack(spacing: 10) {
                ForEach(sorted) { limit in
                    VStack(spacing: 4) {
                        UsageGauge(
                            percentage: limit.percentage,
                            label: limit.label,
                            usedText: formatUsed(limit.used, unit: limit.unit),
                            totalText: limit.total.map { formatUsed($0, unit: limit.unit) },
                            resetAt: limit.resetAt,
                            windowDescription: limit.windowDescription,
                            warningThreshold: Double(config.warningThreshold),
                            dangerThreshold: Double(config.dangerThreshold)
                        )
                        if let details = limit.usageDetails, !details.isEmpty {
                            UsageDetailView(details: details)
                        }
                    }
                }
            }
            .padding(12)
        }
        .frame(maxHeight: 400)
    }

    private var loadingView: some View {
        VStack(spacing: 12) {
            ProgressView()
                .scaleEffect(1.2)
            Text("正在获取用量数据...")
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .frame(height: 120)
    }

    private func errorView(message: String) -> some View {
        VStack(spacing: 8) {
            Image(systemName: "xmark.octagon.fill")
                .font(.largeTitle)
                .foregroundStyle(.red)
            Text(message)
                .font(.caption)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
            Button("重试") {
                Task { await appState.manualRefresh() }
            }
            .buttonStyle(.bordered)
            .controlSize(.small)
        }
        .padding(20)
        .frame(height: 150)
    }

    private var placeholderView: some View {
        VStack(spacing: 8) {
            Text("暂无数据")
                .font(.caption)
                .foregroundStyle(.secondary)
            Button("立即刷新") {
                Task { await appState.manualRefresh() }
            }
            .buttonStyle(.bordered)
            .controlSize(.small)
        }
        .frame(height: 100)
    }

    private var unconfiguredView: some View {
        VStack(spacing: 12) {
            Image(systemName: "gear.badge.questionmark")
                .font(.largeTitle)
                .foregroundStyle(.secondary)
            Text("请先配置 API Key")
                .font(.callout)
            Button("打开设置") {
                SettingsWindowHelper.open()
            }
            .buttonStyle(.borderedProminent)
            .controlSize(.small)
        }
        .padding(20)
        .frame(height: 150)
    }

    // MARK: - Footer

    private var footer: some View {
        HStack {
            Spacer()
            Button {
                Task { await appState.manualRefresh() }
            } label: {
                Image(systemName: "arrow.clockwise")
                    .font(.caption)
            }
            .buttonStyle(.plain)
            .disabled(appState.isRefreshing)

            Button {
                SettingsWindowHelper.open()
            } label: {
                Image(systemName: "gearshape")
                    .font(.caption)
            }
            .buttonStyle(.plain)

            Button {
                SettingsWindowHelper.quit()
            } label: {
                Image(systemName: "power")
                    .font(.caption)
            }
            .buttonStyle(.plain)
        }
        .padding(.horizontal, 16)
        .padding(.vertical, 8)
    }

    // MARK: - Helpers

    private func colorForStatus(_ status: AppStatus) -> Color {
        switch status {
        case .normal: return .green
        case .warning: return .yellow
        case .danger: return .red
        default: return .gray
        }
    }

    private func formatUsed(_ value: Double, unit: String) -> String {
        if unit.isEmpty {
            return Formatters.number(value)
        }
        return "\(Formatters.number(value)) \(unit)"
    }
}
