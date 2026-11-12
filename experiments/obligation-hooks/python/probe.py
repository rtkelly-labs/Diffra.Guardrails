"""Experiment fixture, not a production observer or a Guardrails adapter."""

import json
import os
from pathlib import Path
from uuid import uuid4


class Probe:
    def __init__(self, runner, native_id):
        self.runner = runner
        self.native_id = native_id
        self.invocation = str(uuid4())
        self.mode = None
        self.observer = False
        self.attempts = 0
        self.rows = []
        self.assertions = []
        self.events = ["before-scenario"]

    def configure(self, mode):
        self.mode = mode
        if mode == "setup-fail":
            raise AssertionError("Deliberate setup failure")
        self.observer = mode != "no-observer"

    def read(self):
        self.events.append("operation")
        # Synthetic transport: no network, retries, SDK or real product is exercised.
        self.attempts = 0 if self.mode == "zero" else 4 if self.mode == "over-budget" else 2
        self.rows = ["wrong"] if self.mode == "functional-fail" else ["A", "B"]

    def record(self, obligation, api, observed, expected, passed):
        self.events.append(api)
        self.assertions.append(dict(obligation=obligation, api=api, observed=observed,
                                    expected=expected, passed=passed))
        assert passed, f"{obligation}: expected {expected}, observed {observed}"

    def functional(self):
        if self.mode != "missing-functional":
            self.record("Functional.RowsMatch", "rows_match", self.rows, ["A", "B"],
                        self.rows == ["A", "B"])

    def policy(self):
        if self.mode == "missing-policy":
            return
        if self.mode == "unreachable":
            if self.attempts < 0:  # Deliberately present, valid, unexecuted assertion.
                self.retry_budget()
            return
        if self.mode == "misclassified":
            self.record("Resilience.RetryBudget", "rows_match", self.rows, ["A", "B"], True)
            return
        self.retry_budget()

    def retry_budget(self):
        self.record("Resilience.RetryBudget", "retry_budget", self.attempts, 3, self.attempts <= 3)

    def finish(self):
        self.events.append("after-scenario")
        output = Path(os.environ["PROBE_OUTPUT"]) / self.runner
        output.mkdir(parents=True, exist_ok=True)
        # One file per invocation, so retries and workers cannot overwrite a passing result.
        (output / f"{self.invocation}.json").write_text(json.dumps(dict(
            runner=self.runner, nativeId=self.native_id, invocation=self.invocation,
            case=self.mode, observer=self.observer, completed=True,
            events=self.events, assertions=self.assertions)), encoding="utf-8")

    def cleanup(self):
        self.events.append("fixture-cleanup")
        output = Path(os.environ["PROBE_OUTPUT"]) / self.runner
        (output / f"{self.invocation}.cleanup").write_text(json.dumps(self.events), encoding="utf-8")
