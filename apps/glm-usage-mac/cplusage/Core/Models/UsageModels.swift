import Foundation

enum LimitType: String, Codable, CaseIterable {
    case token = "TOKEN"
    case request = "REQUEST"
    case time = "TIME"
    case cost = "COST"
}

struct UsageSnapshot: Equatable {
    let timestamp: Date
    let providerId: String
    let planName: String
    let planLevel: String

    struct Limit: Equatable, Identifiable {
        var id: UUID = UUID()
        let type: LimitType
        let label: String
        let used: Double
        let total: Double?
        let percentage: Double
        let unit: String
        let resetAt: Date?
        let windowDescription: String
        let usageDetails: [UsageDetail]?
    }

    let limits: [Limit]
}

// MARK: - Codable for UsageSnapshot (manual to avoid UUID issues)

extension UsageSnapshot: Codable {
    private enum CodingKeys: String, CodingKey {
        case timestamp, providerId, planName, planLevel
        case limits
    }

    private struct CodableLimit: Codable {
        let type: LimitType
        let label: String
        let used: Double
        let total: Double?
        let percentage: Double
        let unit: String
        let resetAt: Date?
        let windowDescription: String
        let usageDetails: [UsageDetail]?
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        timestamp = try container.decode(Date.self, forKey: .timestamp)
        providerId = try container.decode(String.self, forKey: .providerId)
        planName = try container.decode(String.self, forKey: .planName)
        planLevel = try container.decode(String.self, forKey: .planLevel)
        let codableLimits = try container.decode([CodableLimit].self, forKey: .limits)
        limits = codableLimits.map {
            Limit(type: $0.type, label: $0.label, used: $0.used, total: $0.total,
                  percentage: $0.percentage, unit: $0.unit, resetAt: $0.resetAt,
                  windowDescription: $0.windowDescription, usageDetails: $0.usageDetails)
        }
    }

    func encode(to encoder: Encoder) throws {
        var container = encoder.container(keyedBy: CodingKeys.self)
        try container.encode(timestamp, forKey: .timestamp)
        try container.encode(providerId, forKey: .providerId)
        try container.encode(planName, forKey: .planName)
        try container.encode(planLevel, forKey: .planLevel)
        let codableLimits = limits.map {
            CodableLimit(type: $0.type, label: $0.label, used: $0.used, total: $0.total,
                         percentage: $0.percentage, unit: $0.unit, resetAt: $0.resetAt,
                         windowDescription: $0.windowDescription, usageDetails: $0.usageDetails)
        }
        try container.encode(codableLimits, forKey: .limits)
    }
}

struct UsageDetail: Equatable, Identifiable, Codable {
    let id: UUID
    let toolName: String
    let usage: Int

    init(id: UUID = UUID(), toolName: String, usage: Int) {
        self.id = id
        self.toolName = toolName
        self.usage = usage
    }

    init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)
        self.id = try container.decodeIfPresent(UUID.self, forKey: .id) ?? UUID()
        self.toolName = try container.decode(String.self, forKey: .toolName)
        self.usage = try container.decode(Int.self, forKey: .usage)
    }

    private enum CodingKeys: String, CodingKey {
        case id, toolName, usage
    }
}

struct UsageRecord: Identifiable, Codable {
    let id: UUID
    let timestamp: Date
    let providerId: String
    let planLevel: String
    let percentage: Double

    init(id: UUID = UUID(), timestamp: Date, providerId: String, planLevel: String, percentage: Double) {
        self.id = id
        self.timestamp = timestamp
        self.providerId = providerId
        self.planLevel = planLevel
        self.percentage = percentage
    }
}
