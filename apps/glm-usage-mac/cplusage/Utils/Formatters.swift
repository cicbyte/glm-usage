import Foundation

enum Formatters {
    private static let numberFormatter: NumberFormatter = {
        let f = NumberFormatter()
        f.numberStyle = .decimal
        return f
    }()

    private static let resetDateFormatter: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "MM/dd HH:mm"
        return f
    }()

    static func percentage(_ value: Double) -> String {
        String(format: "%.0f%%", value)
    }

    static func number(_ value: Double) -> String {
        numberFormatter.string(from: NSNumber(value: value)) ?? "\(Int(value))"
    }

    static func resetTime(from date: Date?) -> String {
        guard let date else { return "未知" }
        return "重置时间 \(resetDateFormatter.string(from: date))"
    }
}
