import SwiftUI
import Combine

struct UsageGauge: View {
    let percentage: Double
    let label: String
    let usedText: String
    let totalText: String?
    let resetAt: Date?
    let windowDescription: String
    let warningThreshold: Double
    let dangerThreshold: Double

    @State private var now = Date()
    private let timer = Timer.publish(every: 1, on: .main, in: .common).autoconnect()

    var body: some View {
        VStack(alignment: .leading, spacing: 10) {
            // 标题行
            HStack {
                Text(label)
                    .font(.subheadline.bold())
                    .foregroundStyle(.primary)
                Spacer()
                Text(Formatters.percentage(percentage))
                    .font(.title3.monospacedDigit().bold())
                    .foregroundStyle(colorForPercentage(percentage))
            }

            // 进度条
            ProgressView(value: percentage, total: 100)
                .progressViewStyle(.linear)
                .tint(colorForPercentage(percentage))
                .scaleEffect(y: 1.5)

            // 用量 + 倒计时
            HStack {
                if let totalText {
                    Text("\(usedText) / \(totalText)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                Spacer()
                if let resetAt {
                    HStack(spacing: 4) {
                        Image(systemName: "clock")
                            .font(.caption2)
                        Text(countdownText(resetAt: resetAt, now: now))
                            .font(.caption.monospacedDigit())
                    }
                    .foregroundStyle(.secondary)
                }
            }

            // 周期 + 重置时间
            HStack(spacing: 4) {
                Text(windowDescription)
                if let resetAt {
                    Text("·")
                    Text(Formatters.resetTime(from: resetAt))
                }
            }
            .font(.caption2)
            .foregroundStyle(.tertiary)
        }
        .padding(14)
        .background(
            RoundedRectangle(cornerRadius: 12)
                .fill(.ultraThinMaterial)
        )
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(colorForPercentage(percentage).opacity(0.3), lineWidth: 1)
        )
        .onReceive(timer) { time in
            now = time
        }
    }

    private func countdownText(resetAt: Date, now: Date) -> String {
        let interval = resetAt.timeIntervalSince(now)
        guard interval > 0 else { return "即将重置" }

        let totalSeconds = Int(interval)
        let days = totalSeconds / 86400
        let hours = (totalSeconds % 86400) / 3600
        let minutes = (totalSeconds % 3600) / 60
        let seconds = totalSeconds % 60

        if days > 0 {
            return String(format: "%dd %dh %dm 后重置", days, hours, minutes)
        } else if hours > 0 {
            return String(format: "%dh %dm %ds 后重置", hours, minutes, seconds)
        } else {
            return String(format: "%dm %ds 后重置", minutes, seconds)
        }
    }

    private func colorForPercentage(_ value: Double) -> Color {
        if value >= dangerThreshold { return .red }
        if value >= warningThreshold { return .yellow }
        return .green
    }
}
