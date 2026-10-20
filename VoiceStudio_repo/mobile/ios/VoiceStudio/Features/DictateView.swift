import SwiftUI
import VoiceStudioKit

struct DictateView: View {
    @Environment(AppModel.self) private var model
    @State private var text = ""
    @State private var transcript = DictationTranscript()
    @State private var previousText = ""
    @State private var checking = true
    @State private var ready = false
    @State private var reason = ""
    @State private var listening = false
    @State private var finalizing = false
    @State private var capturing: Task<Void, Never>?
    @State private var receiving: Task<Void, Never>?
    @State private var audio = DictationAudio()
    @State private var stream = DictationStream()
    @GestureState private var holding = false
    var body: some View {
        Page {
            if checking { ProgressView(model.t("common.loading")) }
            else if !ready { Text(model.t("mobile.dictate.not_ready", ["reason": reason])) }
            Button(model.t(listening ? "common.stop" : "mobile.dictate.tap_to_start"), systemImage: listening ? "stop.fill" : "mic.fill") {
                if listening { stop() } else { start() }
            }.buttonStyle(VSButtonStyle(kind: .primary)).disabled(!ready || finalizing)
                .accessibilityHint(model.t("mobile.dictate.hold_to_talk")).accessibilityIdentifier("dictate.record")
                // One surface: the exclusive gesture prevents a hold's release
                // from also toggling the button. Native accessibility activates its action.
                .highPriorityGesture(
                    LongPressGesture(minimumDuration: 0.2)
                        .sequenced(before: DragGesture(minimumDistance: 0))
                        .exclusively(before: TapGesture())
                        .updating($holding) { value, state, _ in
                            if case .first(.second(true, _)) = value { state = true }
                        }
                        .onEnded { value in
                            if case .second = value { if listening { stop() } else { start() } }
                        }
                )
                .onChange(of: holding) { _, pressed in
                    if pressed { start() }
                    else if listening { stop() }
                }
            if listening { Text(model.t("mobile.dictate.listening")) }
            Text(text).frame(maxWidth: .infinity, alignment: .leading)
                .textSelection(.enabled).accessibilityLabel(model.t("mobile.ios.transcript"))
            Button(model.t("mobile.dictate.copy"), systemImage: "doc.on.doc") { UIPasteboard.general.string = text }.disabled(text.isEmpty)
            ShareLink(item: text) { Label(model.t("mobile.speak.share"), systemImage: "square.and.arrow.up") }.disabled(text.isEmpty)
        }.navigationTitle(model.t("mobile.tabs.dictate"))
        .task { await readiness() }
        .refreshable { await readiness() }
        .onDisappear { capturing?.cancel(); receiving?.cancel(); audio.stop(); listening = false; finalizing = false; Task { await stream.close() } }
    }
    private func readiness() async {
        let scope = model.hostState.scopeID
        checking = true; defer { checking = false }
        do {
            let status = try await model.api().readiness()
            guard scope == model.hostState.scopeID else { return }
            ready = status.ready; reason = status.reason(l10n: model.l10n)
        }
        catch is CancellationError {}
        catch let error as ClientError { guard scope == model.hostState.scopeID else { return }; ready = false; reason = model.t(error.key, error.params); await model.connection.handle(error) }
        catch { ready = false; reason = model.t("mobile.errors.offline") }
    }
    private func start() {
        guard ready, !listening, !finalizing else { return }
        let scope = model.hostState.scopeID
        listening = true; previousText = text; transcript = DictationTranscript()
        capturing = Task {
            // Per session: a finished `receiving` from an earlier session must not count.
            var receiverCreated = false
            do {
                let api = try model.api()
                let messages = try await stream.start(api: api)
                // A stop during startup never creates `receiving`, so nothing else clears `finalizing`.
                guard scope == model.hostState.scopeID, listening, !Task.isCancelled else { finalizing = false; await stream.close(); return }
                receiving = Task {
                    do {
                        for try await message in messages {
                            try Task.checkCancellation()
                            guard scope == model.hostState.scopeID else { throw CancellationError() }
                            transcript.receive(message)
                            text = [previousText, transcript.text].filter { !$0.isEmpty }.joined(separator: "\n")
                            if transcript.finished { audio.stop(); listening = false; finalizing = false }
                        }
                    } catch { audio.stop(); listening = false; finalizing = false; if !Task.isCancelled && scope == model.hostState.scopeID { model.error = error as? ClientError ?? ClientError("mobile.errors.dictation") } }
                }
                receiverCreated = true
                let chunks = try await audio.start()
                guard scope == model.hostState.scopeID, listening, !Task.isCancelled else { audio.stop(); try? await stream.finish(); return }
                for try await chunk in chunks { try Task.checkCancellation(); try await stream.send(chunk) }
                try await stream.finish()
            } catch is CancellationError { if !receiverCreated { finalizing = false } } catch {
                audio.stop(); listening = false; finalizing = false; await stream.close()
                if scope == model.hostState.scopeID { model.error = error as? ClientError ?? ClientError("mobile.errors.audio") }
            }
        }
    }
    private func stop() { finalizing = true; audio.stop(); listening = false }
}
