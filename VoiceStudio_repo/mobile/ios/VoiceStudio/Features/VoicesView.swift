import SwiftUI
import VoiceStudioKit
import UniformTypeIdentifiers

struct VoicesView: View {
    @Environment(AppModel.self) private var model
    @Environment(\.vsTheme) private var theme
    @State private var query = ""
    @State private var creating = false
    @State private var player = AudioPlayer()
    @State private var renamed: Profile?
    @State private var deleting: Profile?
    @State private var name = ""
    @State private var hostScope: UUID?
    private var filtered: [Profile] { model.profiles.filter { query.isEmpty || $0.name.localizedCaseInsensitiveContains(query) } }
    var body: some View {
        Page {
            Button(model.t("mobile.voices.new_clone"), systemImage: "plus") { creating = true }
                .buttonStyle(VSButtonStyle(kind: .primary)).accessibilityIdentifier("voices.create")
            if !model.profiles.isEmpty { SearchField(prompt: model.t("mobile.voices.search"), text: $query) }
            if model.profiles.isEmpty && !model.loading { Text(model.t("mobile.voices.empty")).foregroundStyle(theme.color("textMuted")) }
            else if !model.profiles.isEmpty {
                Card {
                    if filtered.isEmpty { SearchEmptyState(query: query) }
                    ForEach(filtered) { profile in
                        HStack(spacing: VSTokens.spacingGap) {
                            VStack(alignment: .leading, spacing: VSTokens.spacingGap) {
                                Text(profile.name)
                                if let language = model.languages.language(profile.language), !language.automatic {
                                    Text(language.name).font(.custom("InterVariable", size: VSTokens.textLabel.size, relativeTo: .caption)).foregroundStyle(theme.color("textMuted"))
                                }
                            }.frame(maxWidth: .infinity, alignment: .leading)
                            Button(model.t("common.play"), systemImage: "play.fill") { Task { await model.perform { try await player.load(model.api().profileAudio(profile.id), extension: "wav"); player.play() } } }
                                .labelStyle(.iconOnly).buttonStyle(VSButtonStyle(kind: .tertiary))
                            Menu {
                                Button(model.t("mobile.voices.rename"), systemImage: "pencil") { name = profile.name; renamed = profile }
                                Button(model.t("common.delete"), systemImage: "trash", role: .destructive) { deleting = profile }
                            } label: { Label(model.t("mobile.ios.more_actions"), systemImage: "ellipsis") }
                                .labelStyle(.iconOnly).buttonStyle(VSButtonStyle(kind: .tertiary)).frame(minWidth: 44, minHeight: 44)
                                .accessibilityIdentifier("voices.actions." + profile.id)
                        }
                        if profile.id != filtered.last?.id { Divider().overlay(theme.color("border")) }
                    }
                }
            }
            if player.file != nil { AudioPlayerView(player: player) }
        }.navigationTitle(model.t("mobile.tabs.voices")).navigationBarTitleDisplayMode(.large)
        .refreshable { await model.perform { model.profiles = try await model.api().profiles() } }
        .sheet(isPresented: $creating) { NavigationStack { CloneView() }.environment(model).presentationBackground(theme.color("surfaceOverlay")).presentationCornerRadius(VSTokens.radiusLg) }
        .sheet(item: $renamed) { profile in
            NavigationStack {
                Page(backgroundRole: "surfaceOverlay") {
                    TextField(model.t("mobile.voices.name_label"), text: $name).accessibilityIdentifier("voices.rename.name")
                    Button(model.t("common.save")) { Task { await model.perform { try await model.api().rename(profile.id, name: name); model.profiles = try await model.api().profiles(); renamed = nil } } }
                        .buttonStyle(VSButtonStyle(kind: .primary)).disabled(name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || model.busy)
                }.navigationTitle(model.t("mobile.voices.rename"))
                    .toolbar { Button(model.t("common.cancel")) { renamed = nil }.buttonStyle(VSButtonStyle(kind: .tertiary)) }
            }.presentationBackground(theme.color("surfaceOverlay")).presentationCornerRadius(VSTokens.radiusLg)
        }
        .sheet(item: $deleting) { profile in
            DeleteConfirmation(title: model.t("mobile.voices.delete_confirm", ["name": profile.name])) {
                Task { await model.perform { try await model.api().deleteProfile(profile.id); model.profiles = try await model.api().profiles() } }
            }
        }
        .onAppear { hostScope = model.hostState.scopeID }
        .onDisappear { if hostScope != model.hostState.scopeID { player.clear() } else { player.pause() } }
    }
}
struct CloneView: View {
    @Environment(AppModel.self) private var model
    @Environment(\.dismiss) private var dismiss
    @State private var name = ""
    @State private var consent = false
    @State private var importing = false
    @State private var audio: SelectedMedia?
    @State private var recorder = Recorder()
    var body: some View {
        Page(backgroundRole: "surfaceOverlay") {
            Text(model.t("mobile.voices.consent_title"))
            Text(model.t("mobile.voices.consent_body"))
            Toggle(model.t("mobile.voices.consent_confirm"), isOn: $consent).frame(minHeight: 44)
            TextField(model.t("mobile.voices.name_label"), text: $name).accessibilityIdentifier("voices.clone.name")
            if consent {
                Button(model.t(recorder.recording ? "mobile.ios.record_stop" : "mobile.voices.record"), systemImage: recorder.recording ? "stop.fill" : "mic.fill") {
                    Task { await model.perform {
                        if recorder.recording { recorder.stop(); audio = recorder.file.map { SelectedMedia(url: $0) } }
                        else { try await recorder.start() }
                    } }
                }.disabled(model.busy)
                if recorder.recording { Text(model.t("mobile.voices.recording", ["seconds": String(recorder.seconds)])) }
                Button(model.t("mobile.voices.pick_file")) { importing = true }.disabled(recorder.recording)
            }
            if let audio { SelectedFileRow(name: audio.name) { self.audio = nil } }
            Button(model.t("mobile.ios.clone_create")) {
                Task { await model.perform {
                    guard let audio else { return }
                    let access = audio.url.startAccessingSecurityScopedResource(); defer { if access { audio.url.stopAccessingSecurityScopedResource() } }
                    let bytes = try Data(contentsOf: audio.url)
                    _ = try await model.api().clone(name: name, filename: audio.uploadName, audio: bytes)
                    model.profiles = try await model.api().profiles(); dismiss()
                } }
            }.buttonStyle(VSButtonStyle(kind: .primary)).disabled(!consent || name.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty || audio == nil || recorder.recording || model.busy)
        }.navigationTitle(model.t("mobile.voices.new_clone"))
        .toolbar { Button(model.t("common.cancel")) { recorder.stop(); dismiss() } }
        .fileImporter(isPresented: $importing, allowedContentTypes: [.audio]) { result in
            switch result { case .success(let url): audio = SelectedMedia(url: url); case .failure: model.error = ClientError("mobile.errors.file") }
        }
        .onDisappear { recorder.stop() }
    }
}
