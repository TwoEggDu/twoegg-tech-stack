import json
from pathlib import Path


def damage():
    return json.loads(Path(__file__).with_name("damage.json").read_text())["damage"]


def charge_enabled():
    return True


if __name__ == "__main__":
    print(json.dumps({"damage": damage(), "charge": charge_enabled()}, sort_keys=True))
