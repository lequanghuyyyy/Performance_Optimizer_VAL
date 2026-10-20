# VoiceStudio on your phone (iOS and Android)

The iOS and Android apps are **companions** for a VoiceStudio you already run:
the desktop app, a Docker container or a server. Speech, cloning, dubbing and
dictation run on that computer. The phone sends requests only to the computer
you pair it with: no accounts, no cloud relay and no analytics.

> **Preview.** The apps aren't in the App Store or Google Play yet. Build them
> from source (below). On-device models are planned for a later release.

| | iOS | Android |
|---|---|---|
| Minimum OS | iOS 17 (iPhone, iPad) | Android 10 (API 29) |
| Source | [`mobile/ios/`](../mobile/ios/README.md) | [`mobile/android/`](../mobile/android/README.md) |

## What you can do

- **Speak**: type text, pick a voice, engine and language, generate, play,
  share. Recent generations can be starred or deleted.
- **Voices**: browse and preview voices; clone one by recording or picking an
  audio file; rename or delete.
- **Dub**: pick a video or audio file or paste a link, choose target languages
  and a voice, and follow the job. Long jobs keep running on the computer;
  the app picks them up again when reopened.
- **Dictate**: record and watch the transcript appear live.
- **Settings**: saved computers, light/dark mode and the same color palettes
  as the desktop app, language, notifications, privacy and licences.

## Pair the phone

1. On the computer, open VoiceStudio, click the **Local** pill in the footer
   and choose **Network** ([how sharing works](sharing.md)). On a different network, use
   [Tailscale](sharing.md) or an HTTPS address instead.
2. In the app, tap **Scan QR code** and scan the code the panel shows. Or tap
   **Enter address** and type the address and the 6-digit PIN.
3. For a server protected by an [API key](api-auth.md), enter the key once.
   The app swaps it for a session and never stores the key; **Disconnect**
   ends the session.

Plain `http://` works only for addresses on your own network (home Wi-Fi,
`.local`, Tailscale); everything else must be `https://`.

## If something doesn't work

- **"Accept the licence on your computer"**: some models need their licence
  accepted first. Open VoiceStudio on the computer → **Model Catalogue** and
  accept it there; the phone can't accept licences.
- **Engine shown as unavailable**: install it on the computer. The reason the
  app shows comes from the computer.
- **Dictate isn't ready**: install the speech-to-text model the app names, on
  the computer. Remote dictation also needs an API-key session or a trusted
  network set up on the computer; the share PIN alone isn't enough.
- **Can't connect**: make sure the phone and computer are on the same network
  (or both on Tailscale), sharing is on, and you used the IP address the panel
  shows. See [troubleshooting](install/troubleshooting.md).

## For developers

Both apps follow one contract, [`mobile/contract/SPEC.md`](../mobile/contract/SPEC.md):
a feature ships on both or neither. Colors, the API description and the text
are generated from the desktop app and backend, never copied by hand:

```sh
bun scripts/export-mobile-tokens.ts        # theme tokens from the Electron theme
uv run python scripts/export_openapi.py    # mobile/contract/openapi.json from the backend
```

`tests/test_mobile_contract.py` fails when either is stale.

**Try the iPhone app without Xcode.** Each `mobile` workflow run uploads an
unsigned build, `VoiceStudio-ios-unsigned` (kept 7 days). Download it from the
run's Artifacts, then install it with a sideloading tool that signs it with your
own Apple ID (for example Sideloadly). With a free Apple ID the install expires
after 7 days; turn on Developer Mode on the iPhone (Settings → Privacy &
Security) when iOS asks. App text uses the
Electron locale catalogs (`mobile.*` keys). The `mobile` workflow builds and
tests both apps.
