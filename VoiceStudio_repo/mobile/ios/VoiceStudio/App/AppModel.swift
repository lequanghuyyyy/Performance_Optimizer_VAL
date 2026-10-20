import Foundation
import SwiftUI
import Observation
import VoiceStudioKit
import UserNotifications
import BackgroundTasks

@MainActor @Observable final class AppModel {
    let connection: ConnectionStore
    let hostState: HostState
    var dub: DubWorkflow { hostState.dub }
    var profiles: [Profile] { get { hostState.profiles } set { hostState.profiles = newValue } }
    var inventory: EngineInventory? { get { hostState.inventory } set { hostState.inventory = newValue } }
    let languages = LanguageCatalog()
    var loading = false
    var history: [HistoryItem] { get { hostState.history } set { hostState.history = newValue } }
    var dubHistory: [DubHistoryItem] { get { hostState.dubHistory } set { hostState.dubHistory = newValue } }
    var error: ClientError?
    var busy = false
    @ObservationIgnored private var activityCount = 0
    var consentAnswered: Bool
    var analytics: Bool { didSet { UserDefaults.standard.set(analytics, forKey: "analytics") } }
    var notifications: Bool { didSet { UserDefaults.standard.set(notifications, forKey: "notifications") } }
    var language: String { didSet { UserDefaults.standard.set(language, forKey: "language") } }
    var theme: ThemePreferences { didSet { UserDefaults.standard.set(try? JSONEncoder().encode(theme), forKey: "theme") } }
    private(set) var monitorTask: Task<Void, Never>?
    private var notified = Set<String>()
    private var monitorID = UUID()
    let mockMode: Bool
    init() {
        mockMode = ProcessInfo.processInfo.arguments.contains("--ui-smoke")
        let d = UserDefaults.standard
        if mockMode {
            d.removeObject(forKey: "consentAnswered"); d.removeObject(forKey: "activeHost")
            connection = ConnectionStore(defaults: d, vault: SmokeVault(), transport: SmokeTransport())
        } else { connection = ConnectionStore() }
        hostState = HostState(dub: DubWorkflow())
        consentAnswered = d.bool(forKey: "consentAnswered")
        analytics = d.bool(forKey: "analytics"); notifications = d.bool(forKey: "notifications")
        language = d.string(forKey: "language") ?? ""
        theme = d.data(forKey: "theme").flatMap { try? JSONDecoder().decode(ThemePreferences.self, from: $0) } ?? ThemePreferences()
        connection.onHostChange = { [weak self] host in
            guard let self else { return }
            self.pauseMonitor()
            self.hostState.transition(to: host)
            BackgroundUpload.shared.scope(to: host)
            BGTaskScheduler.shared.cancel(taskRequestWithIdentifier: BackgroundMonitor.identifier)
            self.error = nil; self.loading = false; self.notified.removeAll()
        }
    }
    @ObservationIgnored private let translator = L10n()
    var l10n: L10n { var value = translator; value.locale = (language.isEmpty ? Locale.current.identifier : language).replacingOccurrences(of: "_", with: "-"); return value }
    func t(_ key: String, _ params: [String: String] = [:]) -> String { l10n.t(key, params) }
    func chooseConsent(_ choice: Bool) { analytics = choice; consentAnswered = true; UserDefaults.standard.set(true, forKey: "consentAnswered") }
    func api() throws -> APIClient { guard let api = connection.client else { throw ClientError("mobile.errors.offline") }; return api }
    func perform(connectionChange: Bool = false, _ work: @escaping @MainActor () async throws -> Void) async {
        let scope = hostState.scopeID
        activityCount += 1; busy = true
        defer { activityCount -= 1; busy = activityCount > 0 }
        do { if connectionChange { try await work() } else { try await hostState.run(work) } }
        catch is CancellationError {}
        catch let failure as ClientError { guard scope == hostState.scopeID || connectionChange else { return }; error = failure; await connection.handle(failure, connectionChange: connectionChange) }
        catch { if scope == hostState.scopeID { self.error = ClientError("mobile.errors.file") } }
    }
    func refresh() async throws {
        let scope = hostState.scopeID
        loading = true; defer { if scope == hostState.scopeID { loading = false } }
        let client = try api()
        async let p = client.profiles(); async let e = client.engines()
        async let h = client.history(); async let d = client.dubHistory()
        let results = try await (p, e, h, d)
        try Task.checkCancellation()
        guard scope == hostState.scopeID else { throw CancellationError() }
        profiles = results.0; inventory = results.1; history = results.2; dubHistory = results.3
    }
    func monitor() {
        guard monitorTask == nil, let api = connection.client, dub.checkpoint?.host == api.address.baseURL.absoluteString else { return }
        let scope = hostState.scopeID
        let monitorID = UUID(); self.monitorID = monitorID
        monitorTask = Task { [weak self] in
            guard let self else { return }
            defer { if monitorID == self.monitorID { self.monitorTask = nil } }
            do {
                try await self.dub.monitor(api: api)
                try Task.checkCancellation()
                guard scope == self.hostState.scopeID else { return }
                if let cp = self.dub.checkpoint, cp.stage == DubStage.done { await self.notify(cp, failed: false) }
                let history = try await api.dubHistory()
                try Task.checkCancellation()
                guard scope == self.hostState.scopeID else { return }
                self.dubHistory = history
            } catch is CancellationError {
            } catch let failure as ClientError {
                guard scope == self.hostState.scopeID else { return }
                self.error = failure; await self.connection.handle(failure)
                if let cp = self.dub.checkpoint, failure.key == "mobile.errors.dub_failed" { await self.notify(cp, failed: true) }
            } catch { if scope == self.hostState.scopeID { self.error = ClientError("mobile.errors.dub_failed") } }
        }
    }
    func pauseMonitor() { monitorID = UUID(); monitorTask?.cancel(); monitorTask = nil }
    func notify(_ cp: DubCheckpoint, failed: Bool) async {
        let id = cp.host + cp.jobID + (failed ? "failed" : "done")
        guard notifications, !notified.contains(id), !UserDefaults.standard.bool(forKey: id) else { return }
        let center = UNUserNotificationCenter.current()
        let status = await Self.authorizationStatus()
        guard !Task.isCancelled, connection.active?.id == cp.host,
              status == .authorized || status == .provisional else { return }
        let content = UNMutableNotificationContent()
        content.title = t(failed ? "mobile.dub.failed_notification" : "mobile.dub.done_notification", ["title": cp.title])
        content.sound = .default
        do { try await center.add(UNNotificationRequest(identifier: id, content: content, trigger: nil)); notified.insert(id); UserDefaults.standard.set(true, forKey: id) }
        catch { self.error = ClientError("mobile.permissions.denied") }
    }
    // UNNotificationSettings isn't Sendable; read it off the main actor and return only the status.
    nonisolated private static func authorizationStatus() async -> UNAuthorizationStatus {
        await UNUserNotificationCenter.current().notificationSettings().authorizationStatus
    }
    func enableNotifications(_ enabled: Bool) async {
        if !enabled { notifications = false; return }
        do { notifications = try await UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound, .badge])
            if !notifications { self.error = ClientError("mobile.permissions.denied") }
        } catch { self.error = ClientError("mobile.permissions.denied") }
    }
    func acceptUpload() {
        guard let host = connection.active?.id,
              let receipt = BackgroundUpload.shared.takeReceipt(host: host) else { return }
        if let response = receipt.upload, dub.checkpoint?.jobID != response.job_id {
            do {
                try dub.begin(response, api: api(), languages: receipt.pending.languages, profile: receipt.pending.profile)
                monitor()
                BackgroundMonitor.schedule()
            } catch { self.error = ClientError("mobile.errors.dub_failed") }
        } else if let failure = receipt.failure {
            error = failure
            Task {
                guard connection.active?.id == receipt.pending.host else { return }
                await connection.handle(failure)
            }
        }
    }
}
