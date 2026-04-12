#!/usr/bin/env python3
"""Migrate engine-notes/ into 4 new sections: rendering, engine-toolchain, performance, system-design.

Usage:
    python scripts/migrate-sections.py --dry-run   # Preview mapping, no changes
    python scripts/migrate-sections.py              # Execute moves + relref updates
"""

import os
import re
import sys
import shutil
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parent.parent
CONTENT_DIR = ROOT / "content"
ENGINE_NOTES_DIR = CONTENT_DIR / "engine-notes"

# ── Prefix rules (first match wins, so more specific prefixes must come first) ──

PREFIX_RULES = [
    # === rendering ===
    ("unity-rendering-", "rendering"),
    ("unity-shader-variant", "rendering"),
    ("unity-shader-", "rendering"),
    ("unity-shadervariantcollection-", "rendering"),
    ("unity-svc-", "rendering"),
    ("unity-urp-", "rendering"),
    ("unity-what-shadervariantcollection", "rendering"),
    ("unity-why-always-included-shaders", "rendering"),
    ("unity-why-shader-variant", "rendering"),
    ("urp-", "rendering"),
    ("shader-", "rendering"),
    ("cachedshadows-", "rendering"),
    ("dlss-evolution-", "rendering"),
    ("game-graphics-stack-", "rendering"),
    ("graphics-api-", "rendering"),
    ("graphics-math-", "rendering"),
    ("graphics-pipeline-", "rendering"),
    ("math-", "rendering"),
    ("zero-b-deep-", "rendering"),

    # === engine-toolchain (before system-design to catch data-oriented-runtime first) ===
    ("build-debug-", "engine-toolchain"),
    ("unity-script-compilation-pipeline-", "engine-toolchain"),
    ("unity-stripping-", "engine-toolchain"),
    ("hybridclr-", "engine-toolchain"),
    ("il2cpp-", "engine-toolchain"),
    ("unity-asset-system-", "engine-toolchain"),
    ("unity-android-", "engine-toolchain"),
    ("unity-addressables-", "engine-toolchain"),
    ("unity-buildpipeline-", "engine-toolchain"),
    ("storage-io-", "engine-toolchain"),
    ("unity-async-runtime-", "engine-toolchain"),
    ("unity-async-loading-", "engine-toolchain"),
    ("data-oriented-runtime-", "engine-toolchain"),
    ("crash-analysis-", "engine-toolchain"),
    ("unity-resource-", "engine-toolchain"),
    ("unity-why-assetbundle-", "engine-toolchain"),
    ("unity-why-needs-assetbundle-", "engine-toolchain"),
    ("unity-why-resource-", "engine-toolchain"),
    ("unity-assetbundle-", "engine-toolchain"),
    ("unity-assets-", "engine-toolchain"),
    ("unity-builtin-resources-", "engine-toolchain"),
    ("unity-first-package-", "engine-toolchain"),
    ("unity-guid-fileid-", "engine-toolchain"),
    ("unity-how-assets-", "engine-toolchain"),
    ("unity-how-shader-is-stored-", "engine-toolchain"),
    ("unity-how-to-read-resource-", "engine-toolchain"),
    ("unity-importer-", "engine-toolchain"),
    ("unity-level-streaming-", "engine-toolchain"),
    ("unity-loading-", "engine-toolchain"),
    ("unity-player-settings-", "engine-toolchain"),
    ("unity-prefab-", "engine-toolchain"),
    ("unity-resources-", "engine-toolchain"),
    ("unity-scene-", "engine-toolchain"),
    ("unity-scriptableobject-", "engine-toolchain"),
    ("unity-serialized-", "engine-toolchain"),

    # === performance ===
    ("game-budget-", "performance"),
    ("game-performance-", "performance"),
    ("device-tier", "performance"),
    ("mobile-hardware-", "performance"),
    ("mobile-tool-", "performance"),
    ("mobile-platform-", "performance"),
    ("mobile-unity-", "performance"),
    ("hardware-cpu-", "performance"),
    ("cpu-opt-", "performance"),
    ("gpu-opt-", "performance"),
    ("rendering-tier-design-", "performance"),
    ("android-", "performance"),
    ("from-model-table-", "performance"),

    # === system-design (data-oriented-* AFTER data-oriented-runtime-* matched above) ===
    ("game-engine-architecture-", "system-design"),
    ("game-programming-patterns-", "system-design"),
    ("pattern-", "system-design"),
    ("software-engineering-solid-", "system-design"),
    ("solid-sw-", "system-design"),
    ("data-structures-and-algorithms-", "system-design"),
    ("ds-", "system-design"),
    ("dots-", "system-design"),
    ("dod-", "system-design"),
    ("data-oriented-", "system-design"),
    ("unreal-", "system-design"),
    ("ue-", "system-design"),
    ("mass-", "system-design"),
    ("server-ecs-", "system-design"),
    ("sv-ecs-", "system-design"),
    ("skill-system-", "system-design"),
    ("game-backend-", "system-design"),
]

# Files that don't match any prefix pattern
STANDALONE_FILES = {
    "ta-role-boundaries-and-deliverables.md": "rendering",
    "renderdoc-reading-entry.md": "rendering",
    "client-infra-rendering.md": "rendering",
    "player-input-and-game-content-to-screen.md": "rendering",
    "hardware-02-tbdr.md": "rendering",

    "cdn-cache-busting.md": "engine-toolchain",

    "memory-topic-index.md": "performance",
    "platform-development-differences-car-pc-mobile-console.md": "performance",
    "artist-resource-self-check-before-submit.md": "performance",

    "unity-engine-insights.md": "system-design",
    "engine-programmer-reading-entry.md": "system-design",
    "client-programmer-reading-entry.md": "system-design",
}

SKIP_FILES = {"_index.md"}


def classify(filename):
    """Return target section for a file, or None if unmatched."""
    if filename in SKIP_FILES:
        return None
    if filename in STANDALONE_FILES:
        return STANDALONE_FILES[filename]
    for prefix, section in PREFIX_RULES:
        if filename.startswith(prefix):
            return section
    return None


def build_mapping():
    """Build {filename: target_section} for all engine-notes files."""
    mapping = {}
    unmatched = []
    for f in sorted(ENGINE_NOTES_DIR.glob("*.md")):
        name = f.name
        if name in SKIP_FILES:
            continue
        section = classify(name)
        if section:
            mapping[name] = section
        else:
            unmatched.append(name)
    return mapping, unmatched


def move_files(mapping, dry_run=False):
    """Move files from engine-notes/ to their target sections."""
    for section in {"rendering", "engine-toolchain", "performance", "system-design"}:
        target = CONTENT_DIR / section
        if not dry_run:
            target.mkdir(exist_ok=True)

    moved = 0
    for filename, section in sorted(mapping.items()):
        src = ENGINE_NOTES_DIR / filename
        dst = CONTENT_DIR / section / filename
        if dry_run:
            print(f"  {filename} -> {section}/")
        else:
            shutil.move(str(src), str(dst))
        moved += 1
    return moved


def update_relrefs(mapping, dry_run=False):
    """Update relref links in ALL .md files under content/."""
    # Build lookup: filename -> new section
    lookup = {name: section for name, section in mapping.items()}

    pattern = re.compile(r'(relref\s+")(engine-notes/)([^"]+\.md)(")')
    updated_files = 0
    updated_refs = 0

    for md_file in sorted(CONTENT_DIR.rglob("*.md")):
        text = md_file.read_text(encoding="utf-8")
        new_text = text

        def replacer(m):
            nonlocal updated_refs
            prefix = m.group(1)      # 'relref "'
            filename = m.group(3)    # 'some-file.md'
            suffix = m.group(4)      # '"'
            if filename in lookup:
                updated_refs += 1
                return f'{prefix}{lookup[filename]}/{filename}{suffix}'
            # If not in mapping, leave unchanged (might be _index.md or unknown)
            return m.group(0)

        new_text = pattern.sub(replacer, new_text)

        if new_text != text:
            updated_files += 1
            if dry_run:
                # Show first few changes per file
                for m in pattern.finditer(text):
                    fn = m.group(3)
                    if fn in lookup:
                        print(f"  {md_file.relative_to(ROOT)}: engine-notes/{fn} -> {lookup[fn]}/{fn}")
                        break  # just show first per file
            else:
                md_file.write_text(new_text, encoding="utf-8")

    return updated_files, updated_refs


def main():
    dry_run = "--dry-run" in sys.argv

    if dry_run:
        print("=== DRY RUN (no changes will be made) ===\n")

    # Build mapping
    mapping, unmatched = build_mapping()

    # Report counts by section
    counts = defaultdict(int)
    for section in mapping.values():
        counts[section] += 1

    print(f"File mapping ({len(mapping)} files):")
    for section in ["rendering", "engine-toolchain", "performance", "system-design"]:
        print(f"  {section}: {counts[section]} files")

    if unmatched:
        print(f"\n!!! UNMATCHED FILES ({len(unmatched)}) — need manual assignment:")
        for f in unmatched:
            print(f"  {f}")
        if not dry_run:
            print("\nAborting: fix unmatched files before running without --dry-run")
            sys.exit(1)

    # Move files
    print(f"\n--- Moving files ---")
    moved = move_files(mapping, dry_run)
    print(f"{'Would move' if dry_run else 'Moved'}: {moved} files")

    # Update relrefs
    print(f"\n--- Updating relref links ---")
    updated_files, updated_refs = update_relrefs(mapping, dry_run)
    print(f"{'Would update' if dry_run else 'Updated'}: {updated_refs} refs in {updated_files} files")

    if not dry_run and not unmatched:
        # Remove old engine-notes _index.md and directory
        index_file = ENGINE_NOTES_DIR / "_index.md"
        if index_file.exists():
            index_file.unlink()
        remaining = list(ENGINE_NOTES_DIR.glob("*"))
        if not remaining:
            ENGINE_NOTES_DIR.rmdir()
            print(f"\nRemoved empty directory: content/engine-notes/")
        else:
            print(f"\n!!! content/engine-notes/ still has {len(remaining)} files:")
            for f in remaining:
                print(f"  {f.name}")

    print("\nDone.")


if __name__ == "__main__":
    main()
