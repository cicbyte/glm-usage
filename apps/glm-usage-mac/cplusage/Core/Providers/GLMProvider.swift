import Foundation
import Combine

final class GLMProvider: Provider {
    let id = "glm"
    let name = "GLM"
    let icon = "cpu"

    @Published var isLoading: Bool = false
    @Published var lastError: String?
    @Published var currentUsage: UsageSnapshot?

    private var apiKey: String?
    private var baseURL: String?

    var isConfigured: Bool {
        apiKey != nil && !apiKey!.isEmpty
    }

    func configure(apiKey: String, baseURL: String?) {
        self.apiKey = apiKey
        self.baseURL = baseURL ?? ConfigService.shared.serverEndpoint.rawValue
    }

    func fetchUsage() async throws -> UsageSnapshot {
        guard let apiKey else {
            throw APIError.invalidAPIKey
        }

        let base = baseURL ?? ConfigService.shared.serverEndpoint.rawValue
        let urlString = "\(base)/api/monitor/usage/quota/limit"

        let response: GLMQuotaResponse = try await APIService.shared.request(
            url: urlString,
            apiKey: apiKey
        )

        // Check business error
        if response.code != 200 {
            throw APIError.businessError(
                code: response.code,
                message: errorMessage(for: response.code)
            )
        }

        guard let data = response.data else {
            throw APIError.decodingError("响应数据为空")
        }

        let snapshot = try mapToSnapshot(data)
        return snapshot
    }

    func validateConfig() async -> Bool {
        do {
            _ = try await fetchUsage()
            return true
        } catch {
            return false
        }
    }

    // MARK: - Mapping

    private func mapToSnapshot(_ data: GLMQuotaData) throws -> UsageSnapshot {
        let planLevel = data.level ?? "unknown"
        let planName = GLMPlanLevel(rawValue: planLevel)?.displayName ?? planLevel

        var limits: [UsageSnapshot.Limit] = []

        if let glmLimits = data.limits {
            for glmLimit in glmLimits {
                let limitType = mapLimitType(glmLimit.type)
                let percentage = Double(glmLimit.percentage ?? 0)
                let resetAt = glmLimit.nextResetTime.map {
                    Date(timeIntervalSince1970: Double($0) / 1000.0)
                }

                let label: String
                let unit: String
                let windowDesc: String
                switch limitType {
                case .time:
                    label = "MCP 调用"
                    unit = "次"
                    windowDesc = "每月重置"
                case .token:
                    label = "Token 消耗"
                    unit = "tokens"
                    windowDesc = "每 5 小时重置"
                default:
                    label = "限额"
                    unit = ""
                    windowDesc = ""
                }

                let details = glmLimit.usageDetails?.compactMap { detail -> UsageDetail? in
                    guard let code = detail.modelCode, let usage = detail.usage else { return nil }
                    return UsageDetail(toolName: code, usage: usage)
                }

                let limit = UsageSnapshot.Limit(
                    type: limitType,
                    label: label,
                    used: Double(glmLimit.currentValue ?? 0),
                    total: glmLimit.usage.map { Double($0) },
                    percentage: percentage,
                    unit: unit,
                    resetAt: resetAt,
                    windowDescription: windowDesc,
                    usageDetails: details
                )
                limits.append(limit)
            }
        }

        return UsageSnapshot(
            timestamp: Date(),
            providerId: id,
            planName: planName,
            planLevel: planLevel,
            limits: limits
        )
    }

    private func mapLimitType(_ type: String?) -> LimitType {
        switch type {
        case "TIME_LIMIT": return .time
        case "TOKENS_LIMIT": return .token
        default: return .request
        }
    }

    private func errorMessage(for code: Int) -> String {
        switch code {
        case 401: return "API Key 无效，请检查设置"
        case 429: return "请求太频繁，请稍后再试"
        case 403: return "账号权限不足"
        default: return "请求失败 (错误码: \(code))"
        }
    }
}
