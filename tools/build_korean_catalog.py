#!/usr/bin/env python3
"""Merge reviewed source translations into the stable-id Korean catalog."""

from __future__ import annotations

import argparse
import json
import pathlib


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", required=True, type=pathlib.Path)
    parser.add_argument("--reviewed", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()

    source = json.loads(args.source.read_text(encoding="utf-8"))
    reviewed = json.loads(args.reviewed.read_text(encoding="utf-8"))
    known_sources = {entry["source"] for entry in source["entries"]}
    unused = sorted(set(reviewed) - known_sources)
    if unused:
        raise ValueError("reviewed source is not present in extraction: " + repr(unused))

    entries = []
    for source_entry in source["entries"]:
        korean = reviewed.get(source_entry["source"], "")
        entries.append(
            {
                "id": source_entry["id"],
                "korean": korean,
                "status": "reviewed" if korean else "pending",
            }
        )

    output = {
        "schemaVersion": 1,
        "targetLocale": "ko",
        "entries": entries,
    }
    args.output.write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    reviewed_count = sum(entry["status"] == "reviewed" for entry in entries)
    print(f"Built Korean catalog: {reviewed_count}/{len(entries)} reviewed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
