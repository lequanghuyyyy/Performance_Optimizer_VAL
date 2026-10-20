# VoiceStudio Android companion (M1)

Native Kotlin / Jetpack Compose companion for a paired VoiceStudio host. No
inference, cloud relay, analytics transmission, crash reporter or push service.

## Prerequisites

- JDK 21 (AGP does not support the machine's default JDK 27).
- Android SDK platform 36 and build tools; minimum device API 29.
- Internet for the first Gradle/dependency download; no NDK required.
- A VoiceStudio host with Network sharing enabled, or a private server/Tailscale
  URL. Engine installation, model license acceptance and host administration
  remain on the host.

From this directory:

```sh
export JAVA_HOME=/opt/homebrew/opt/openjdk@21
export ANDROID_HOME=/opt/homebrew/share/android-commandlinetools
# Elsewhere, use your own JDK and SDK paths.
printf 'sdk.dir=%s\n' "$ANDROID_HOME" > local.properties
./gradlew assembleDebug
./gradlew testDebugUnitTest
./gradlew lint
./gradlew assembleDebugAndroidTest
./gradlew installDebug
adb shell am start -n com.voicestudio.mobile/.MainActivity
```

Windows: use `gradlew.bat` and equivalent JAVA_HOME / ANDROID_HOME settings.
`local.properties`, build outputs and local IDE state are ignored. Gradle 8.13,
AGP 8.12.0, Kotlin 2.2.21 and direct dependencies are pinned in the wrapper and
`gradle/libs.versions.toml`. The wrapper was generated using installed Gradle
9.8.1. Compilation runs inside Gradle to avoid Kotlin daemon files outside
sandbox writable directories; parallel workers are capped for this shared host.
The version name/code are derived at configuration time from root `package.json`.

## Layout

- `app`: launcher, Navigation Compose, consent, generated asset packaging, JVM UI
  smoke/contract tests.
- `core/designsystem`: immutable generated `VSTokens`, token-to-Material role
  mapping, shapes, Inter typography, appearance preferences.
- `core/network`: typed REST responses and multipart operations, guarded OkHttp
  transport, private-address policy, cancellable requests and ticketed dictation.
- `core/data`: DataStore preferences/saved hosts, AES-GCM/Android Keystore session
  vault, durable WorkManager dub pipeline and local notifications.
- `core/l10n`: nested catalog lookup, interpolation, locale/plural fallback.
- `features`: separate `connect`, `speak`, `voices`, `dub`, `dictate`, `settings`
  packages with shared native controls, playback, capture and permission helpers.
  The six feature packages share one Gradle module to avoid six identical build
  configurations and cross-module UI plumbing in this companion-sized app.

## Screens and behavior

Before pairing: consent (dismissal means No), Connect, and Settings About/Privacy.
Connect supports CameraX/ZXing QR scanning, manual URL/PIN/API-key entry, saved
hosts, labels and last-seen timestamps. Public `/health` must carry the backend
marker and expected status/version; `/profiles` verifies consumption access.
Master keys only enter the session exchange and are never persisted. Session
and PIN ciphertext is stored using a Keystore AES-GCM key; DataStore contains
only connection metadata/preferences and dub checkpoints. A 401 returns to
pairing when it belongs to the active connection; a failed attempt on another
host leaves the active session intact. Disconnect revokes an administrator
session when one exists and clears local access even offline. PIN-only pairing
has no session to revoke. Reconnecting retains a saved host's label and updates
its last-seen timestamp. Unreadable preferences or malformed persisted JSON
fall back to defaults rather than preventing launch.

Connecting/reconnecting resets host data, the navigation tree and its saved
forms, pending document targets, players and dictation controllers. Switching to
a different host or disconnecting cancels the old host's WorkManager jobs and
clears dub checkpoints; checkpoint keys also include the host, and late writes
from inactive hosts are rejected. Reconnecting to the same host retains durable
job monitoring. Long jobs already running on the old computer continue there.

All forms resize their scroll viewport for the keyboard. Background taps,
primary/secondary actions and navigation dismiss focus; single-line fields have
the native Done action. The multiline Speak editor keeps Return for newlines
and offers a visible Done toolbar while focused. Rename and picker dialogs use
their own window's focus manager. Android retains its normal keyboard-first Back
handling; reaching Generate only requires scrolling or Done.

Picker labels take their intrinsic single-line width; values fill the remaining
space, align to the trailing edge and ellipsize to one line. The full value
remains in the accessibility description, and choice lists allow two lines per
name. This applies to voice, engine, language, palette and dub targets. Every
searchable list shows the query when no matches remain.

Tabs render their content without navigation fades. Language catalogs and locale
mapping load off the UI thread and are cached; Speak and Dub show inline loading
indicators with their controls already visible. History/profile refreshes show
inline progress rather than hiding the screen. Dictate keeps its controls visible
while checking readiness.

Speak has text, backend voice/engine/language pickers, generation, host-streamed
Media3 playback, an accessible seek/progress slider, system share/save, and
recent history with star/delete. Voices has search, previews, rename/delete,
permission-gated recording or file selection and explicit clone consent.

Dub keeps a device document's `OpenableColumns.DISPLAY_NAME` separately from its
private cached path, shows that name, and sends it as the upload filename so host
history retains it. Remove deselects the private copy and names the file for
accessibility. History uses grouped title/metadata rows with compact, named save
and share icons. Save opens the system document picker; Share opens the share
sheet.

Dub accepts device media or a link for the host to ingest, multiple target
languages and an explicit voice choice: **Default voice (engine preset)** or a
saved profile. There is no source-voice choice. Desktop source cloning uses
`dub-session.ts:readTranscript` and `/dub/transcribe-stream/{job_id}` to prepare
`cast_sources` / `speaker_clones`, then `applySpeakerCloneDefaults` assigns
`auto:` profile IDs carried into `/dub/generate/{job_id}` by `segmentGenInputs`.
The synchronous `/dub/transcribe/{job_id}` used by M1 returns no cast-reference
metadata. Both mobile apps therefore use the honest preset/profile choice;
an empty `profile_id` explicitly means the engine preset, never source cloning.

Before creating a job, Dub fetches `/engines/translation`, selects exactly its
`active` ID, and requires positive `installed` and `ready` fields. Missing or
unready engines direct the user to VoiceStudio Settings on the computer. An
`online` engine names the service and requires confirmation for that job using
equally styled Allow/Decline buttons; dismissal and decline start no upload or
translation. LLM engines also require that consent unless the resolved
`dub_translation` skill and provider metadata positively identify a loopback
endpoint on the host. `/api/settings/llm-skills` and `/api/settings/llm-providers`
expose this metadata on authorized server hosts; desktop LAN hosts forbid these
admin reads, so an unknown LLM route requires consent. The public translation
inventory's `configured_via` names the service when available. Provider names and
the provider's `local` flag alone do not prove locality: base URLs can be
overridden. Only literal loopback IPs and `localhost` qualify, without any DNS or
provider calls from the phone. Approval is scoped to a single durable job. The
worker rechecks the active provider and a fingerprint of its resolved endpoint
before starting/resuming and immediately before every translation request, and
refuses changed routing; every
`/dub/translate` body contains the explicit authorized `provider`. Existing jobs
without provider metadata fail with recovery advice instead of using a fallback.
WorkManager copies no credentials into job input. It
uploads, waits for prep, transcribes, translates each track, starts generation,
and replays persisted job events while polling. Checkpoints resume existing host
tasks after app process death or connectivity loss. Abort stops the host before
cancelling the worker. Downloads use the guarded client and a FileProvider share.
The worker requests a data-sync foreground service for long uploads. If Android
refuses that start, work continues within WorkManager's normal execution limits;
cancellation still stops it. The merged manifest declares `FOREGROUND_SERVICE`,
`FOREGROUND_SERVICE_DATA_SYNC` and a `dataSync` WorkManager service. Notification
permission is not required to upload/promote the service; it gates optional
completion notifications. Security failures and initialization/input failures
produce actionable Dub errors. Failed job receipts are shown once and acknowledged
in DataStore across screen recreation, app restarts and host switches. Cancelled
jobs show a cancellation status instead of a failure. Successful job download
actions remain available. Optional completion notifications are generated locally,
without a push service.

Dictate checks readiness and streams mono 16 kHz signed PCM over the host
WebSocket, using a one-use ticket for sessions. Tap/hold controls, partial/final
transcripts, copy/share and lifecycle-based microphone cleanup are included.
Sherpa utterance finals append without stopping capture. Only summary/legacy EOF
finals or socket termination end a session; authoritative/refined summary text
replaces the live transcript without duplication, and empty summaries preserve
committed phrases. Unknown final kinds are ignored, matching desktop. Callbacks
and cleanup are scoped to the recording generation, so an old stream ending
cannot close the next session.
Readiness names the missing ASR model and its size, with the desktop install
location. API-key/trusted-network advice appears only after an authorization
failure; a share PIN alone does not authorize dictation. Settings includes live appearance, per-appearance palettes,
system/desktop languages, notification permission, privacy, version and licenses.

## Shared inputs

Run these from the repository root only when intentionally regenerating shared
inputs (this Android implementation does not edit them):

```sh
uv run python scripts/export_openapi.py
bun scripts/export-mobile-tokens.ts
```

Gradle's `:app:bundleCatalogs` copies all Electron locale JSON files and
`mobile/contract/strings*.en.json` into generated assets on every input change.
It also bundles `electron/src/shared/languages.json` (the canonical display
names, with Auto first) and generates the manifest label from English `app.name`.
Only the selected locale's fallback chain is parsed, with catalogs and L10n
instances cached per locale. Chinese script/region tags map to the bundled
zh-CN (Hans) and zh-TW (Hant, HK, MO) catalogs before English fallback.
No copied catalogs are checked in. New Android-only keys live in
`mobile/contract/strings.android-additions.en.json`; untranslated mobile keys
fall back to English until the maintainer integrates their translations.
The checked-in generated token file is never edited by Android development.
OpenAPI is read as the contract; typed models reflect loose responses verified
against backend router implementations. No guessed endpoints are used.

Inter 4.1 variable TTF comes from the official rsms/inter GitHub release.
The release zip SHA-256 is
`9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e`.
`OFL.txt`, `Apache-2.0.txt` and dependency notices are bundled under licenses and
available offline in About.

## Contract decisions and limits

- Android network-security XML cannot express IP CIDRs. The XML enables the
  transport; the sole OkHttp client is the security boundary: HTTP URLs must be
  loopback, RFC1918, CGNAT, IPv4/IPv6 link-local, IPv6 ULA, `.localhost`, `.local`
  or `.ts.net`. DNS results for HTTP must also be private, preventing rebinding
  to a public address. Other hosts require HTTPS with system trust. Redirects,
  unpaired origins, URL credentials and credential query parameters are rejected.
  Media3 uses this same client; no WebView/DownloadManager bypass exists.
- There is no `/languages` route. Both pickers use the build-bundled desktop
  `languages.json`; Speak defaults to Auto, or a selected profile's language.
  Dub excludes Auto, maps names to ISO codes through Android locale data when
  available, and preserves the canonical name for long-tail languages. The
  host determines which languages its selected translator/engine supports.
- `/engines` is loosely typed in OpenAPI; its router supplies `display_name`,
  `active`, `available`, `reason`, `hint` and `supports_cloning`. Select the active
  engine when eligible, otherwise an eligible fallback. A reference-bearing
  profile requires positive cloning support. Default voice sends no `profile_id`.
- Palette labels come directly from generated tokens, except Default uses the
  desktop `themeAppearance.default` translation. The large native screen title
  derives its scale from editor/UI tokens; controls/cards use 8/10 dp token radii.
- `/generate` returns a WAV with `X-Audio-*` headers, not JSON. Playback streams
  its canonical `/audio/...` host file. The progress scrubber satisfies the
  waveform/progress option; no synthetic waveform is fabricated.
- Dub is the host's ingest/prep → transcribe → translate → generate pipeline,
  with `/jobs/{id}/events` replay and `/jobs/{id}` status. WorkManager retries
  transient transport/service failures and keeps completed stage checkpoints.
  The backend has no atomic idempotency key for starting generation; a process
  dying after host acceptance but before checkpoint persistence can repeat that
  stage. Remote jobs themselves continue on the host.
- Model-license navigation opens the paired host UI, where model selection and
  license acceptance live. The companion does not expose admin setup endpoints.
- No blur or custom motion is used. Compose follows the system animation scale;
  all content scrolls, text scales, touch targets are at least 48 dp. Large font
  scales show labelled navigation icons without squeezed tab text. A visible
  text field or backend-provided name is used for each interactive choice.

## Verification

With JDK 21 and Android SDK 36:
`./gradlew assembleDebug`, `./gradlew testDebugUnitTest` (81 tests, zero failures or skips), and `./gradlew lint` all passed. Tests used `HF_HUB_OFFLINE=1` with an
empty HF cache; all API/WebSocket traffic was against local mock servers.

JVM tests cover token parity for every palette/role, L10n interpolation, plural
categories and fallback, every literal UI/error key, pairing and cleartext
boundaries, mock-server health/PIN/session exchange/revoke/401, binary generation,
upload/progress, origin isolation and cancellation. A Robolectric Compose smoke
flow covers launch → consent → mock host pairing → generation → Play; its player
is a test double, so it verifies the playback command rather than audible output.

Regressions cover explicit translation providers, missing/unready
engines, named-service consent and decline, consent on durable resume, changed
providers, explicit preset/profile dub request bodies, scripted multi-phrase
WebSocket transcripts and EOF/refined summaries, host switching/reconnection,
player/form disposal, checkpoint isolation and old-host worker cancellation.
AndroidX WorkManager's pinned test helper initializes its JVM scheduler.
Additional tests exercise foreground-service refusal and cancellation, storage
read/decode recovery, saved labels, script-based Chinese locales, per-locale
catalog caching, once-only failure receipts, candidate connection isolation,
PIN-only disconnect and stale dictation cleanup. A real file worker traverses
promotion, multipart upload, prep, transcription, translation and generation
against a mock host with notification permission denied, asserting the original
filename and the manifest's service permissions/type. Its foreground updater is
a test double; Android 15 service promotion still needs device verification.
Tests also cover worker
security failures, invalid job input, local/remote/unknown LLM routes and route
changes, intrinsic picker width, no-match queries, document display names, Remove
accessibility, compact history actions and first-frame controls while language
loading is deliberately suspended.

Device camera,
microphone, audible playback, permission prompts, real-host inference, foreground
upload survival and notification delivery still require emulator/device and host
validation. Existing instrumentation covers ART interpolation, bundled language
data, structured error copy/duration formatting, and Activity launch. No device
commands or instrumented tests were run; the required build, JVM
tests and lint run with JDK 21 and SDK 36.

Robolectric uses the API 34 instrumented SDK pinned in the version catalog.
Gradle resolves it into its normal dependency cache and copies it into
`app/build/robolectric-sdk`; the test runner then resolves SDK jars offline,
without writing Robolectric locks or Maven downloads into the user home.
