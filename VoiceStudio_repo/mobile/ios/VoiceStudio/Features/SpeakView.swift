import SwiftUI
import VoiceStudioKit

struct VoicePicker: View {
    @Environment(AppModel.self) private var model
    @Binding var selection: String
    var dubbing = false
    var body: some View {
        ChoicePicker(label: model.t("mobile.speak.voice"), selection: $selection, options:
            (dubbing ? [SelectionOption(id: DubVoiceChoice.unselected, title: model.t("mobile.dub.choose_voice"), enabled: false)] : []) +
            [SelectionOption(id: "", title: model.t(DubVoiceChoice.defaultLabelKey))] +
            model.profiles.map { SelectionOption(id: $0.id, title: $0.name, subtitle: model.languages.language($0.language)?.name) })
    }
}
struct SpeakView: View {
    @Environment(AppModel.self) private var model
    @Environment(\.vsTheme) private var theme
    @State private var text = ""
    @State private var selection = SpeechSelection()
    @State private var player = AudioPlayer()
    @State private var generating = false
    @State private var deleting: HistoryItem?
    @State private var hostScope: UUID?
    private var engines: [SelectionOption] {
        (model.inventory?.tts.backends ?? []).map {
            SelectionOption(id: $0.id, title: $0.display_name,
                            subtitle: $0.explanation(for: selection.profile(in: model.profiles), l10n: model.l10n),
                            enabled: $0.selectable(for: selection.profile(in: model.profiles)))
        }
    }
    var body: some View {
        Page {
            TextEditor(text: $text).frame(minHeight: VSTokens.textEditor.lineHeight * 6)
                .scrollContentBackground(.hidden)
                .overlay(alignment: .topLeading) {
                    if text.isEmpty {
                        Text(model.t("mobile.speak.placeholder"))
                            .foregroundStyle(theme.color("placeholder"))
                            .padding(.horizontal, VSTokens.spacingGap / 3)
                            .padding(.vertical, VSTokens.spacingGap / 2)
                            .allowsHitTesting(false).accessibilityHidden(true)
                    }
                }
                .padding(Theme.compactInset)
                .background(theme.color("input"), in: RoundedRectangle(cornerRadius: VSTokens.radiusControl))
                .accessibilityLabel(model.t("mobile.speak.placeholder")).accessibilityIdentifier("speak.text")
            Card {
                VoicePicker(selection: Binding(get: { selection.profileID }, set: {
                    selection.chooseProfile($0, profiles: model.profiles, inventory: model.inventory, catalog: model.languages)
                }))
                Divider().overlay(theme.color("border"))
                ChoicePicker(label: model.t("mobile.speak.engine"), selection: $selection.engineID, options: engines).accessibilityIdentifier("speak.engine")
                if let explanation = engines.first(where: { $0.id == selection.engineID })?.subtitle {
                    Text(explanation).foregroundStyle(theme.color("textMuted"))
                }
                Divider().overlay(theme.color("border"))
                ChoicePicker(label: model.t("mobile.speak.language"), selection: $selection.language,
                             options: model.languages.languages.map { SelectionOption(id: $0.value, title: $0.name) }).accessibilityIdentifier("speak.language")
            }
            Button(model.t(generating ? "mobile.speak.generating" : "mobile.speak.generate")) {
                Task {
                    generating = true; defer { generating = false }
                    await model.perform {
                        let result = try await model.hostState.generate(api: model.api(), text: text, profile: selection.profileID, engine: selection.engineID, language: selection.language)
                        try await player.load(result.audio, extension: "wav")
                        model.history = try await model.api().history()
                    }
                }
            }.buttonStyle(VSButtonStyle(kind: .primary))
                .disabled(model.busy || !selection.canGenerate(in: model.inventory, profiles: model.profiles) || text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                .accessibilityIdentifier("speak.generate")
            if player.file != nil || player.playing { AudioPlayerView(player: player) }
            Text(model.t("mobile.speak.recent")).font(.custom("InterVariable", size: Theme.controlTypeSize, relativeTo: .headline))
            if model.history.isEmpty && !model.loading { Text(model.t("mobile.speak.empty_recent")).foregroundStyle(theme.color("textMuted")) }
            else {
                Card {
                    ForEach(model.history) { item in
                        HStack(spacing: VSTokens.spacingGap) {
                            Text(item.text).frame(maxWidth: .infinity, alignment: .leading)
                            Button(model.t("common.play"), systemImage: "play.fill") { Task { await model.perform { let api = try model.api(); try await player.stream(request: api.historyAudioRequest(item.id), api: api) } } }
                                .labelStyle(.iconOnly).buttonStyle(VSButtonStyle(kind: .tertiary))
                            Menu {
                                Button(model.t(item.starred ? "mobile.speak.unstar" : "mobile.speak.star"), systemImage: item.starred ? "star.fill" : "star") { Task { await model.perform { try await model.api().star(item); model.history = try await model.api().history() } } }
                                Button(model.t("common.delete"), systemImage: "trash", role: .destructive) { deleting = item }
                            } label: { Label(model.t("mobile.ios.more_actions"), systemImage: "ellipsis") }
                                .labelStyle(.iconOnly).buttonStyle(VSButtonStyle(kind: .tertiary)).frame(minWidth: 44, minHeight: 44)
                        }
                        if item.id != model.history.last?.id { Divider().overlay(theme.color("border")) }
                    }
                }
            }
        }.navigationTitle(model.t("mobile.tabs.speak")).navigationBarTitleDisplayMode(.large)
        .onChange(of: model.inventory?.tts.active, initial: true) { _, _ in selection.reconcile(inventory: model.inventory, profiles: model.profiles) }
        .onChange(of: model.profiles.map(\.id)) { _, _ in selection.reconcile(inventory: model.inventory, profiles: model.profiles) }
        .refreshable { await model.perform { try await model.refresh() }; selection.reconcile(inventory: model.inventory, profiles: model.profiles) }
        .sheet(item: $deleting) { item in
            DeleteConfirmation(title: model.t("mobile.ios.delete_generation")) {
                Task { await model.perform { try await model.api().deleteHistory(item.id); model.history = try await model.api().history() } }
            }
        }
        .onAppear { hostScope = model.hostState.scopeID }
        .onDisappear { if hostScope != model.hostState.scopeID { player.clear() } else { player.pause() } }
    }
}
