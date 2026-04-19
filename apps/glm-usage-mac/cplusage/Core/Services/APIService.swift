import Foundation

enum APIError: LocalizedError {
    case invalidURL
    case invalidAPIKey
    case unauthorized
    case forbidden
    case rateLimited
    case serverError(String)
    case networkError(String)
    case decodingError(String)
    case businessError(code: Int, message: String)

    var errorDescription: String? {
        switch self {
        case .invalidURL: return "无效的 URL"
        case .invalidAPIKey: return "API Key 格式错误"
        case .unauthorized: return "API Key 无效，请检查设置"
        case .forbidden: return "账号权限不足"
        case .rateLimited: return "请求太频繁，请稍后再试"
        case .serverError(let msg): return "服务暂时不可用: \(msg)"
        case .networkError(let msg): return "网络连接失败: \(msg)"
        case .decodingError(let msg): return "数据解析失败: \(msg)"
        case .businessError(_, let message): return message
        }
    }

    var isRetryable: Bool {
        switch self {
        case .networkError: return true
        case .serverError: return true
        case .rateLimited: return true
        default: return false
        }
    }
}

struct APIService {
    static let shared = APIService()

    private let session: URLSession
    private let maxRetries = 2
    private let baseRetryDelay: TimeInterval = 2.0

    private init() {
        let config = URLSessionConfiguration.default
        config.timeoutIntervalForRequest = 15
        config.timeoutIntervalForResource = 30
        session = URLSession(configuration: config)
    }

    func request<T: Decodable>(url: String, apiKey: String) async throws -> T {
        guard let url = URL(string: url) else {
            throw APIError.invalidURL
        }

        var lastError: Error?

        for attempt in 0...maxRetries {
            do {
                return try await performRequest(url: url, apiKey: apiKey)
            } catch let error as APIError {
                lastError = error
                guard error.isRetryable, attempt < maxRetries else { break }
                let delay = baseRetryDelay * pow(2.0, Double(attempt))
                try await Task.sleep(for: .seconds(delay))
            } catch {
                lastError = error
                guard attempt < maxRetries else { break }
                let delay = baseRetryDelay * pow(2.0, Double(attempt))
                try await Task.sleep(for: .seconds(delay))
            }
        }

        throw lastError ?? APIError.networkError("未知错误")
    }

    private func performRequest<T: Decodable>(url: URL, apiKey: String) async throws -> T {
        var request = URLRequest(url: url)
        request.httpMethod = "GET"
        request.setValue("Bearer \(apiKey)", forHTTPHeaderField: "Authorization")
        request.setValue("CPlusage/\(Constants.App.version)", forHTTPHeaderField: "User-Agent")
        request.setValue("application/json", forHTTPHeaderField: "Accept")

        let data: Data
        let response: URLResponse

        do {
            (data, response) = try await session.data(for: request)
        } catch {
            throw APIError.networkError(error.localizedDescription)
        }

        guard let httpResponse = response as? HTTPURLResponse else {
            throw APIError.networkError("无效的响应")
        }

        switch httpResponse.statusCode {
        case 401:
            throw APIError.unauthorized
        case 403:
            throw APIError.forbidden
        case 429:
            throw APIError.rateLimited
        case 500...599:
            throw APIError.serverError("HTTP \(httpResponse.statusCode)")
        default:
            break
        }

        do {
            let decoder = JSONDecoder()
            return try decoder.decode(T.self, from: data)
        } catch {
            throw APIError.decodingError(error.localizedDescription)
        }
    }
}
