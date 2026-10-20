#!/usr/bin/env python3
"""Copy authoritative catalogs; localize OS permission prompts; derive version from root."""
import argparse
import json
import pathlib
import plistlib
import re
import shutil

ROOT = pathlib.Path(__file__).resolve().parents[3]
IOS = ROOT / "mobile/ios"


def merge(base, addition):
    for key, value in addition.items():
        if isinstance(value, dict) and isinstance(base.get(key), dict):
            merge(base[key], value)
        else:
            base[key] = value


def lookup(catalog, key):
    value = catalog
    for part in key.split("."):
        value = value.get(part, {}) if isinstance(value, dict) else {}
    return value if isinstance(value, str) else None


def prepare(output, info):
    output.mkdir(parents=True, exist_ok=True)
    catalogs = output / "Catalogs"
    catalogs.mkdir(exist_ok=True)
    source = ROOT / "electron/src/renderer/src/i18n/locales"
    shutil.copy2(ROOT / "electron/src/shared/languages.json", catalogs / "languages.json")
    code_source = (ROOT / "electron/src/shared/utils/languages.js").read_text()
    codes = {label: code for code, label in re.findall(r"\{ code: '([^']+)', label: '([^']+)' \}", code_source)}
    if not codes:
        raise ValueError("Desktop LANG_CODES format changed")
    (catalogs / "language-codes.json").write_text(json.dumps(codes, ensure_ascii=True))
    english = json.loads((source / "en.json").read_text())
    additions = sorted((ROOT / "mobile/contract").glob("strings*.en.json"))
    for path in additions:
        merge(english, json.loads(path.read_text()))
        shutil.copy2(path, catalogs / path.name)
    permissions = {
        "CFBundleDisplayName": "mobile.ios.app_name",
        "NSCameraUsageDescription": "mobile.permissions.camera",
        "NSMicrophoneUsageDescription": "mobile.permissions.microphone",
        "NSLocalNetworkUsageDescription": "mobile.permissions.local_network",
    }
    for path in sorted(source.glob("*.json")):
        shutil.copy2(path, catalogs / path.name)
        locale = json.loads(path.read_text())
        localized = output / (path.stem + ".lproj")
        localized.mkdir(exist_ok=True)
        # JSON string syntax provides the escaping required by .strings files.
        lines = [f'{json.dumps(k)} = {json.dumps(lookup(locale, v) or lookup(english, v), ensure_ascii=True)};' for k, v in permissions.items()]
        (localized / "InfoPlist.strings").write_text("\n".join(lines) + "\n")
    version = json.loads((ROOT / "package.json").read_text())["version"]
    data = plistlib.loads((IOS / "VoiceStudio/Info.plist").read_bytes())
    data["CFBundleShortVersionString"] = version
    data["CFBundleVersion"] = "1"
    data["CFBundleLocalizations"] = sorted(path.stem for path in source.glob("*.json"))
    for field, key in permissions.items():
        data[field] = lookup(english, key)
    info.parent.mkdir(parents=True, exist_ok=True)
    info.write_bytes(plistlib.dumps(data))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, type=pathlib.Path)
    parser.add_argument("--info-output", required=True, type=pathlib.Path)
    args = parser.parse_args()
    prepare(args.output, args.info_output)
