import Foundation

final class HistoryManager {
    static let shared = HistoryManager()

    private let defaults = UserDefaults.standard
    private let historyKey = "com.cplusage.usageHistory"

    private init() {}

    func addRecord(_ record: UsageRecord) {
        var records = loadRecords()
        records.append(record)
        // Keep last 7 days
        let cutoff = Calendar.current.date(byAdding: .day, value: -7, to: Date()) ?? Date()
        records = records.filter { $0.timestamp > cutoff }
        saveRecords(records)
    }

    func loadRecords() -> [UsageRecord] {
        guard let data = defaults.data(forKey: historyKey) else { return [] }
        return (try? JSONDecoder().decode([UsageRecord].self, from: data)) ?? []
    }

    private func saveRecords(_ records: [UsageRecord]) {
        guard let data = try? JSONEncoder().encode(records) else { return }
        defaults.set(data, forKey: historyKey)
    }
}
