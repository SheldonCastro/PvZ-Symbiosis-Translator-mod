from __future__ import annotations
import argparse
import subprocess
from pathlib import Path

def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], text=True, encoding="utf-8").strip()

def main() -> int:
    parser = argparse.ArgumentParser(description="Generate evidence-based draft notes from Git and CHANGELOG.")
    parser.add_argument("--version", required=True)
    parser.add_argument("--commit", default="HEAD")
    parser.add_argument("--previous-tag")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    commit = git("rev-parse", f"{args.commit}^{{commit}}")
    range_spec = f"{args.previous_tag}..{commit}" if args.previous_tag else commit
    subjects = git("log", "--format=%h %s", range_spec).splitlines()
    changelog = Path("CHANGELOG.md").read_text(encoding="utf-8")
    unreleased = changelog.split("## [Unreleased]", 1)[-1].split("\n## [", 1)[0].strip()
    body = [
        f"# PvZ Symbiosis Translator {args.version}",
        "",
        f"Private source commit: `{commit}`",
        "",
        "## Changelog",
        "",
        unreleased or "No Unreleased section was found.",
        "",
        "## Git changes",
        "",
        *[f"- {line}" for line in subjects],
        "",
        "These are draft notes generated only from the maintained changelog and Git subjects; review before publication.",
    ]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(body) + "\n", encoding="utf-8")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
