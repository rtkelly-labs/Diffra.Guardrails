"""Ordinary-test control using the exact same observation/assertion fixture."""
import json
from pathlib import Path
import pytest

CASES = json.loads((Path(__file__).parent.parent / "cases.json").read_text())


@pytest.mark.remote_io
@pytest.mark.requirement_read_rows
@pytest.mark.parametrize("mode", [case["case"] for case in CASES])
def test_plain(request, mode):
    probe = request.node.probe
    probe.configure(mode)
    probe.read()
    probe.functional()
    probe.policy()
