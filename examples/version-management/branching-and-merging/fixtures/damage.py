import json
from pathlib import Path


def report():
    root = Path(__file__).resolve().parent
    base = json.loads((root / "base.json").read_text())["damage"]
    percent = json.loads((root / "charge.json").read_text())["percent"]
    raw = base * percent // 100
    return {"base": base, "percent": percent, "raw": raw, "final": raw, "cap": 150}


if __name__ == "__main__":
    print(json.dumps(report(), sort_keys=True))
