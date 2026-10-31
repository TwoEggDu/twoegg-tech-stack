"""Fixed teaching contract. Kept byte-for-byte unchanged across every branch and fix."""
import json
import sys

from damage import report

data = report()
print(json.dumps(data, sort_keys=True))
errors = []
if data["raw"] != data["base"] * data["percent"] // 100:
    errors.append("RAW_FORMULA_MISMATCH")
if data["cap"] != 150:
    errors.append("CAP_CHANGED")
if not 0 <= data["final"] <= 150:
    errors.append("FINAL_DAMAGE_OUT_OF_RANGE")
if data["final"] != min(data["raw"], 150):
    errors.append("FINAL_DAMAGE_NOT_CAPPED_AS_SPECIFIED")
if errors:
    print(",".join(errors), file=sys.stderr)
    raise SystemExit(1)
