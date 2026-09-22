#!/usr/bin/env python3
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

EXPECTED_VERSION = "0.2.0-alpha.6.7.44.41"
EXPECTED_BRANCH = "v0.2-alpha6-7-44-41-capture-guard-scope-fix"
OLD_VERSION = "0.2.0-alpha.6.7.44.40"

VERSION_PATTERNS = [
    re.compile(r"Team Up!\\s+([0-9A-Za-z.\\-]+)\\s+by Ronvotri"),
    re.compile(r"Team Up!\\s+v?([0-9A-Za-z.\\-]+)\\s+loaded\\."),
]
BUILD_PATTERN = re.compile(r"\\[TeamUpBuild\\]\\s+version=([^\\s]+)\\s+branch=([^\\r\\n]+)")
HOOK_PATTERN = re.compile(r"elite capture guard enabled:.*?hooks=(\\d+)", re.IGNORECASE)
SAVE_PATTERN = re.compile(r"Context: loaded save '([^']+)'")
BAD_METHOD_PATTERN = re.compile(
    r"capture guard skipped .*?\\.(ToString|PrintMembers|GetHashCode|Equals|Deconstruct|Dispose|Clone):",
    re.IGNORECASE,
)


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8", errors="replace")


def last_match(pattern: re.Pattern[str], text: str):
    found = pattern.findall(text)
    return found[-1] if found else None


def extract_version(text: str) -> str | None:
    build = BUILD_PATTERN.findall(text)
    if build:
        return build[-1][0]
    for pattern in VERSION_PATTERNS:
        found = pattern.findall(text)
        if found:
            return found[-1]
    return None


def extract_branch(text: str) -> str | None:
    build = BUILD_PATTERN.findall(text)
    return build[-1][1].strip() if build else None


def analyze(text: str) -> tuple[str, list[str]]:
    version = extract_version(text)
    branch = extract_branch(text)
    hooks_raw = last_match(HOOK_PATTERN, text)
    hooks = int(hooks_raw) if hooks_raw is not None else None
    invalid_count = sum(
        1
        for line in text.splitlines()
        if "Team Up!" in line
        and "capture guard" in line
        and "InvalidProgramException" in line
    )
    bad_methods = BAD_METHOD_PATTERN.findall(text)
    saves = SAVE_PATTERN.findall(text)
    save_name = saves[-1] if saves else None

    details: list[str] = []
    details.append(f"detected_version={version or 'UNKNOWN'}")
    details.append(f"detected_branch={branch or 'UNKNOWN'}")
    details.append(f"capture_guard_hooks={hooks if hooks is not None else 'UNKNOWN'}")
    details.append(f"capture_guard_invalid_program_count={invalid_count}")
    details.append(f"object_record_bad_targets={len(bad_methods)}")
    details.append(f"save_loaded={save_name or 'NO'}")

    if version != EXPECTED_VERSION:
        details.append(
            f"VERSION_MISMATCH: expected {EXPECTED_VERSION}; this log is not a valid 6.7.44.41 runtime test."
        )
        if version == OLD_VERSION:
            details.append(
                "OLD_BUILD_CONFIRMED: 6.7.44.40 is still installed/loaded. Replace the Team Up folder before retesting."
            )
        if hooks == 172 and invalid_count > 0:
            details.append(
                "OLD_CRASH_SIGNATURE_CONFIRMED: hooks=172 with capture-guard InvalidProgramException spam."
            )
        return "FAIL_WRONG_BUILD", details

    if branch is not None and branch != EXPECTED_BRANCH:
        details.append(f"BRANCH_MISMATCH: expected {EXPECTED_BRANCH}.")
        return "FAIL_WRONG_BUILD", details

    if invalid_count > 0:
        details.append(
            "CAPTURE_GUARD_FAIL: InvalidProgramException is still present in Team Up capture-guard patching."
        )
        return "FAIL_CAPTURE_GUARD", details

    if bad_methods:
        details.append(
            "CAPTURE_GUARD_FAIL: object/record plumbing methods were still targeted."
        )
        return "FAIL_CAPTURE_GUARD", details

    if hooks is None:
        details.append(
            "INCOMPLETE: 6.7.44.41 identity found, but no elite capture guard hooks= line was found."
        )
        return "INCOMPLETE", details

    if hooks >= 172:
        details.append(
            "CAPTURE_GUARD_FAIL: hook count did not fall below the known 6.7.44.40 value of 172."
        )
        return "FAIL_CAPTURE_GUARD", details

    if save_name is None:
        details.append(
            "INCOMPLETE: startup looks cleaner, but no loaded-save marker is present yet."
        )
        return "INCOMPLETE", details

    details.append(
        "LOAD_GATE_CANDIDATE_PASS: correct 6.7.44.41 identity, no capture-guard InvalidProgramException spam, hooks below 172, and a save-load marker is present."
    )
    details.append(
        "NOTE: this tool cannot prove the game remained stable after the log ended. Ron still must confirm the save stayed open before Runtime PASS."
    )
    return "CANDIDATE_PASS_AWAITING_USER_CONFIRMATION", details


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Read-only Team Up 6.7.44.41 live-load gate inspector for SMAPI-latest.txt."
    )
    parser.add_argument("log", type=Path, help="Path to SMAPI-latest.txt")
    parser.add_argument(
        "--tail",
        type=int,
        default=40,
        help="Print this many final log lines (default: 40)",
    )
    args = parser.parse_args()

    if not args.log.is_file():
        print(f"ERROR: log not found: {args.log}", file=sys.stderr)
        return 2

    text = read_text(args.log)
    status, details = analyze(text)

    print(f"TEAMUP_674441_RUNTIME_GATE={status}")
    for detail in details:
        print(detail)

    if args.tail > 0:
        lines = text.splitlines()
        print(f"\\n--- LOG TAIL ({min(args.tail, len(lines))} lines) ---")
        for line in lines[-args.tail:]:
            print(line)

    return 0 if status == "CANDIDATE_PASS_AWAITING_USER_CONFIRMATION" else 1


if __name__ == "__main__":
    raise SystemExit(main())
