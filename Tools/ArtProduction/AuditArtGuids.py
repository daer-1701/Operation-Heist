"""Find direct and transitive references to original and production art."""
import argparse
from collections import defaultdict, deque
from pathlib import Path
import re


GUID_LINE = re.compile(rb"(?m)^guid: ([\da-f]{32})\s*$")
GUID_REF = re.compile(rb"\bguid: ([\da-f]{32})\b")
ART_DIRS = (
    "Assets/04_Models", "Assets/02_Prefabs/Production", "Assets/02_Prefabs/Art/CampaignArt",
    "Assets/03_Materials", "Assets/05_Textures",
)


def audit(project: Path, output: Path) -> None:
    assets = project / "Assets"
    paths_by_guid = {}
    by_path = {}
    edges = {}
    incoming = defaultdict(set)
    for meta in sorted(assets.rglob("*.meta")):
        match = GUID_LINE.search(meta.read_bytes())
        path = meta.with_suffix("")
        if not match or not path.is_file():
            continue
        key = match[1]
        if key in paths_by_guid:
            raise ValueError(f"GUID duplicado: {path}, {paths_by_guid[key]}")
        paths_by_guid[key] = path
        by_path[path] = key
    for path in by_path:
        if path.suffix not in {".unity", ".prefab", ".mat", ".asset", ".controller"}:
            continue
        text = path.read_bytes()
        if not text.startswith(b"%YAML"):
            continue
        edges[path] = {
            paths_by_guid[guid] for guid in GUID_REF.findall(text)
            if guid in paths_by_guid and paths_by_guid[guid] != path
        }
        for child in edges[path]:
            incoming[child].add(path)

    scenes_for = defaultdict(set)
    for scene in (path for path in edges if path.suffix == ".unity"):
        found = {scene}
        remaining = deque([scene])
        while remaining:
            parent = remaining.popleft()
            for child in edges.get(parent, ()):
                if child not in found:
                    found.add(child)
                    remaining.append(child)
                    scenes_for[child].add(scene)

    selected = [path for path in by_path if path.relative_to(project).as_posix().startswith(ART_DIRS)]
    lines = ["ruta\tguid\tescenas_directas\tprefabs_directos\totros_directos\tescenas_transitivas"]
    for path in sorted(selected):
        def list_paths(paths):
            return ";".join(item.relative_to(project).as_posix() for item in sorted(paths))

        direct = incoming[path]
        lines.append("\t".join((
            path.relative_to(project).as_posix(), by_path[path].decode("ascii"),
            list_paths(p for p in direct if p.suffix == ".unity"),
            list_paths(p for p in direct if p.suffix == ".prefab"),
            list_paths(p for p in direct if p.suffix not in {".prefab", ".unity"}),
            list_paths(scenes_for[path]),
        )))
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"GUID_ART_OK {len(selected)}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("project", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    audit(args.project.resolve(), args.output)
