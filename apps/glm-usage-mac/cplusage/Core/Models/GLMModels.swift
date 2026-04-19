import Foundation

// MARK: - GLM API Response Models

struct GLMQuotaResponse: Codable {
    let code: Int
    let msg: String?
    let data: GLMQuotaData?
    let success: Bool?
}

struct GLMQuotaData: Codable {
    let limits: [GLMLimit]?
    let level: String?
}

struct GLMLimit: Codable {
    let type: String?
    let unit: Int?
    let number: Int?
    let usage: Int?
    let currentValue: Int?
    let remaining: Int?
    let percentage: Int?
    let nextResetTime: Int64?
    let usageDetails: [GLMUsageDetail]?
}

struct GLMUsageDetail: Codable {
    let modelCode: String?
    let usage: Int?
}

// MARK: - GLM Plan Level

enum GLMPlanLevel: String {
    case lite
    case pro
    case max
    case unknown

    var displayName: String {
        switch self {
        case .lite: return "Lite"
        case .pro: return "Pro"
        case .max: return "Max"
        case .unknown: return "Unknown"
        }
    }
}
