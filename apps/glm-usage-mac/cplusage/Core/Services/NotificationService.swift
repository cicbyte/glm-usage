import Foundation
import UserNotifications

final class NotificationService {
    static let shared = NotificationService()

    private init() {}

    func requestAuthorization() async -> Bool {
        do {
            return try await UNUserNotificationCenter.current()
                .requestAuthorization(options: [.alert, .sound])
        } catch {
            return false
        }
    }

    func sendUsageWarning(percentage: Double, provider: String) {
        let content = UNMutableNotificationContent()
        content.title = "CPlusage 用量警告"
        content.body = "\(provider) 用量已达 \(Formatters.percentage(percentage))，请注意控制使用"
        content.sound = .default

        let request = UNNotificationRequest(
            identifier: "usage-warning-\(Date().timeIntervalSince1970)",
            content: content,
            trigger: nil
        )

        UNUserNotificationCenter.current().add(request) { _ in }
    }

    func sendUsageDanger(percentage: Double, provider: String) {
        let content = UNMutableNotificationContent()
        content.title = "CPlusage 用量危险"
        content.body = "\(provider) 用量已达 \(Formatters.percentage(percentage))，即将耗尽！"
        content.sound = .defaultCritical

        let request = UNNotificationRequest(
            identifier: "usage-danger-\(Date().timeIntervalSince1970)",
            content: content,
            trigger: nil
        )

        UNUserNotificationCenter.current().add(request) { _ in }
    }
}
