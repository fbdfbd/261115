"""Export/check saved WorkCategory SO assets against CSV and a readable JSON snapshot.

Usage: python Tools/work_document_data.py export
       python Tools/work_document_data.py check
JSON is a review snapshot; Unity continues to read the ScriptableObject assets.
Only the known WorkCategory Unity YAML layout is supported (no third-party packages).
"""
import argparse
import csv
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
GAME = ROOT / "Assets/WorkGame"
SNAPSHOT = GAME / "Samples/WorkCategories.json"
RULES = ["Highest", "Lowest"]
DISPLAYS = ["Number", "VerticalBar", "HorizontalBar", "Stars", "Grade"]
CATEGORY_FIELDS = {"Id", "Title", "ShortName", "HudColor", "Target"}
METRIC_FIELDS = {"Title", "Agency", "Name", "HighlightWords", "Unit", "Rule", "Display", "Min", "Max", "ScaleMax"}


def scalar(raw):
    raw = raw.strip()
    if raw.startswith('"'):
        # Unity writes YAML's \xNN escapes as well as JSON-compatible \uNNNN.
        return json.loads(re.sub(r"\\x([0-9a-fA-F]{2})", r"\\u00\1", raw))
    if raw.startswith("'"):
        assert raw.endswith("'"), raw
        return raw[1:-1].replace("''", "'")
    return raw


def fields(text, allowed):
    result = {}
    lines = text.splitlines()
    index = 0
    while index < len(lines):
        line = lines[index]
        match = re.match(r"^\s+(?:- )?(\w+):\s*(.*)$", line)
        index += 1
        if not match:
            raise ValueError(f"Unsupported category YAML line: {line!r}")
        key, raw = match.groups()
        if key not in allowed or key in result:
            raise ValueError(f"Unexpected or duplicate field: {key}")
        if key == "HighlightWords":
            words = []
            if raw == "[]":
                result[key] = words
                continue
            assert not raw, raw
            while index < len(lines) and lines[index].startswith("    - "):
                word = lines[index][6:]
                index += 1
                while index < len(lines) and lines[index].startswith("      "):
                    word += " " + lines[index].strip()
                    index += 1
                words.append(scalar(word))
            result[key] = words
            continue
        # Unity folds long double-quoted strings onto six-space continuation lines.
        while index < len(lines) and lines[index].startswith("      "):
            raw += " " + lines[index].strip()
            index += 1
        if key == "HudColor":
            result[key] = {k: float(v) for k, v in re.findall(r"([rgba]): ([\d.eE+-]+)", raw)}
            assert set(result[key]) == set("rgba"), raw
        elif key in {"Rule", "Display", "Min", "Max", "ScaleMax"}:
            value = int(raw)
            result[key] = RULES[value] if key == "Rule" else DISPLAYS[value] if key == "Display" else value
        else:
            result[key] = scalar(raw)
    if set(result) != allowed:
        raise ValueError(f"Missing fields: {allowed - set(result)}")
    return result


def read_asset(path):
    text = path.read_text(encoding="utf-8-sig")
    header, metrics = text.split("  Metrics:\n", 1)
    category = fields(header[header.index("  Id:"):], CATEGORY_FIELDS)
    blocks = re.split(r"(?=^  - Title:)", metrics, flags=re.M)
    category["Metrics"] = [fields(block.rstrip(), METRIC_FIELDS) for block in blocks if block.strip()]
    return category


def read_assets():
    return [read_asset(path) for path in sorted((GAME / "Data").glob("Category_*.asset"))]


def read_csv():
    categories = {}
    with (GAME / "Samples/Categories.csv").open(encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            basic = {key: row[key] for key in ("Id", "Title", "ShortName", "Target")}
            category = categories.setdefault(row["Id"], {**basic, "Metrics": []})
            assert all(category[key] == value for key, value in basic.items()), row["Id"]
            category["Metrics"].append({
                "Title": row["PaperTitle"], "Agency": row["Agency"], "Name": row["Metric"],
                "HighlightWords": [word.strip() for word in row["HighlightWords"].split("|") if word.strip()],
                "Unit": row["Unit"], "Rule": row["Rule"], "Display": row["Display"],
                **{key: int(row[key]) for key in ("Min", "Max", "ScaleMax")},
            })
    return list(categories.values())


def differences(actual, expected, path="Categories"):
    if type(actual) is not type(expected):
        return [{"path": path, "actual": actual, "expected": expected}]
    result = []
    if isinstance(actual, dict):
        for key in sorted(actual.keys() | expected.keys()):
            if key not in actual or key not in expected:
                result.append({"path": path + "." + key, "actual": actual.get(key), "expected": expected.get(key)})
            else:
                result.extend(differences(actual[key], expected[key], path + "." + key))
    elif isinstance(actual, list):
        if len(actual) != len(expected):
            result.append({"path": path + ".length", "actual": len(actual), "expected": len(expected)})
        for index, (left, right) in enumerate(zip(actual, expected), 1):
            result.extend(differences(left, right, f"{path}[{index}]"))
    elif actual != expected:
        result.append({"path": path, "actual": actual, "expected": expected})
    return result


def audit(categories, check_json=True):
    without_colors = [{key: value for key, value in c.items() if key != "HudColor"} for c in categories]
    report = {
        "categoryCount": len(categories), "metricCount": sum(len(c["Metrics"]) for c in categories),
        "csvDifferences": differences(without_colors, read_csv()),
    }
    if check_json:
        report["jsonDifferences"] = differences(categories, json.loads(SNAPSHOT.read_text(encoding="utf-8"))["Categories"]) if SNAPSHOT.exists() else [{"path": "WorkCategories.json", "error": "missing"}]
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["export", "check"])
    args = parser.parse_args()
    categories = read_assets()
    if args.action == "export":
        SNAPSHOT.write_text(json.dumps({
            "Description": "실제 SO를 내보낸 확인용 스냅샷입니다. JSON 편집은 게임에 자동 반영되지 않습니다.",
            "Categories": categories,
        }, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    report = audit(categories)
    print(json.dumps(report, ensure_ascii=True, indent=2))
    return 1 if report["csvDifferences"] or report["jsonDifferences"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
