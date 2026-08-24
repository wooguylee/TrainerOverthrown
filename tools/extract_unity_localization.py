#!/usr/bin/env python3
"""Read Overthrown Unity Localization bundles without modifying the game."""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
from typing import Any, Iterable


def _table_name(name: str) -> str:
    for suffix in (" Shared Data", "_en"):
        if name.endswith(suffix):
            return name[: -len(suffix)]
    return name


def join_tables(
    shared_tables: Iterable[dict[str, Any]],
    english_tables: Iterable[dict[str, Any]],
) -> list[dict[str, Any]]:
    shared_by_table: dict[str, dict[int, str]] = {}
    for table in shared_tables:
        table_name = _table_name(str(table.get("m_Name", "")))
        keys: dict[int, str] = {}
        for entry in table.get("m_Entries", []):
            key_id = int(entry["m_Id"])
            keys[key_id] = str(entry.get("m_Key", ""))
        shared_by_table[table_name] = keys

    result: list[dict[str, Any]] = []
    seen_ids: set[str] = set()
    for table in english_tables:
        table_name = _table_name(str(table.get("m_Name", "")))
        shared_keys = shared_by_table.get(table_name, {})
        for entry in table.get("m_TableData", []):
            key_id = int(entry["m_Id"])
            key = shared_keys.get(key_id) or f"key-{key_id}"
            stable_id = f"{table_name}/{key}"
            if stable_id in seen_ids:
                raise ValueError(f"duplicate stable id: {stable_id}")
            seen_ids.add(stable_id)
            result.append(
                {
                    "id": stable_id,
                    "table": table_name,
                    "key": key,
                    "keyId": key_id,
                    "source": str(entry.get("m_Localized", "")),
                }
            )

    return sorted(result, key=lambda item: item["id"])


def _read_mono_behaviour_trees(bundle_path: pathlib.Path) -> list[dict[str, Any]]:
    import UnityPy  # Imported only for the real bundle extraction path.

    environment = UnityPy.load(str(bundle_path))
    tables: list[dict[str, Any]] = []
    for unity_object in environment.objects:
        if unity_object.type.name != "MonoBehaviour":
            continue
        tree = unity_object.read_typetree()
        if "m_Entries" in tree or "m_TableData" in tree:
            tables.append(tree)
    return tables


def _sha256(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest().upper()


def extract(english_bundle: pathlib.Path, shared_bundle: pathlib.Path) -> dict[str, Any]:
    shared_tables = _read_mono_behaviour_trees(shared_bundle)
    english_tables = _read_mono_behaviour_trees(english_bundle)
    entries = join_tables(shared_tables, english_tables)
    return {
        "schemaVersion": 1,
        "sourceLocale": "en",
        "inputs": {
            "englishBundleSha256": _sha256(english_bundle),
            "sharedBundleSha256": _sha256(shared_bundle),
        },
        "entryCount": len(entries),
        "entries": entries,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--english-bundle", required=True, type=pathlib.Path)
    parser.add_argument("--shared-bundle", required=True, type=pathlib.Path)
    parser.add_argument("--output", required=True, type=pathlib.Path)
    args = parser.parse_args()

    result = extract(args.english_bundle, args.shared_bundle)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Extracted {result['entryCount']} entries to {args.output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
