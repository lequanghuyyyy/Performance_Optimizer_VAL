"""The mobile apps build against committed, generated contracts.

Both fail when the source they were generated from changes without a
regeneration in the same PR, so iOS and Android never drift from the backend
API or the desktop theme.
"""
from __future__ import annotations

import json
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parent.parent
CONTRACT = ROOT / "mobile" / "contract"

sys.path.insert(0, str(ROOT / "scripts"))
import export_openapi  # noqa: E402


def test_openapi_contract_matches_backend():
    committed = (CONTRACT / "openapi.json").read_text(encoding="utf-8")
    assert committed == export_openapi.render(export_openapi.build_schema()), (
        "mobile/contract/openapi.json is stale; run `uv run python scripts/export_openapi.py`"
    )


def test_openapi_operation_ids_are_unique():
    schema = json.loads((CONTRACT / "openapi.json").read_text(encoding="utf-8"))
    ids = [op["operationId"] for ops in schema["paths"].values() for op in ops.values() if "operationId" in op]
    assert len(ids) == len(set(ids))


def _electron_deps_installed() -> bool:
    if shutil.which("bun") is None:
        return False
    probe = subprocess.run(
        ["bun", "--no-install", "-e", "Bun.resolveSync('tailwindcss/theme.css', process.argv[1])", str(ROOT / "electron")],
        capture_output=True, timeout=60,
    )
    return probe.returncode == 0


# ci.yml also runs `bun scripts/export-mobile-tokens.ts --check` after
# `bun install`, so this skip can't hide drift on CI.
@pytest.mark.skipif(not _electron_deps_installed(), reason="needs bun and installed Electron deps")
def test_mobile_tokens_match_electron_theme():
    result = subprocess.run(
        ["bun", "--no-install", "scripts/export-mobile-tokens.ts", "--check"],
        cwd=ROOT, capture_output=True, text=True, timeout=120,
    )
    assert result.returncode == 0, result.stderr


def test_mobile_string_keys_are_namespaced():
    for path in CONTRACT.glob("strings*.en.json"):
        data = json.loads(path.read_text(encoding="utf-8"))
        assert list(data) == ["mobile"], f"{path.name}: new mobile keys belong under `mobile.`"


def test_openapi_contract_is_version_neutral():
    # The app version differs by environment and changes every release; a
    # contract that embedded it would go stale on every version bump.
    schema = {"info": {"version": "9.8.7"}, "components": {"x": {"default": "9.8.7"}}}
    rendered = json.loads(export_openapi.render(schema))
    assert "9.8.7" not in json.dumps(rendered)
    committed = json.loads((CONTRACT / "openapi.json").read_text(encoding="utf-8"))
    assert committed["info"]["version"] == export_openapi.VERSION_PLACEHOLDER
