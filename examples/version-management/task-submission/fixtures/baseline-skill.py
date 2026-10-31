import json
from pathlib import Path


def damage():
    return 100


def charge_enabled():
    return False


if __name__ == "__main__":
    print(json.dumps({"damage": damage(), "charge": charge_enabled()}, sort_keys=True))
