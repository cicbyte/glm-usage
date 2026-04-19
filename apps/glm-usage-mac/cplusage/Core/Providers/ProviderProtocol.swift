import Foundation
import Combine

protocol Provider: Identifiable, ObservableObject {
    var id: String { get }
    var name: String { get }
    var icon: String { get }
    var isConfigured: Bool { get }
    var isLoading: Bool { get set }
    var lastError: String? { get set }
    var currentUsage: UsageSnapshot? { get set }

    func configure(apiKey: String, baseURL: String?)
    func fetchUsage() async throws -> UsageSnapshot
    func validateConfig() async -> Bool
}
