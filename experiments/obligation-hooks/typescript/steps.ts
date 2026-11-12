import { Before, After, Given, When, Then, World, setWorldConstructor } from '@cucumber/cucumber';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { randomUUID } from 'node:crypto';

class Probe extends World {
  mode = '';
  invocation = randomUUID();
  nativeId = '';
  observer = false;
  attempts = 0;
  rows: string[] = [];
  assertions: object[] = [];
  events: string[] = [];

  record(obligation: string, api: string, observed: unknown, expected: unknown, passed: boolean) {
    this.events.push(api);
    this.assertions.push({ obligation, api, observed, expected, passed });
    if (!passed) throw new Error(`${obligation}: expected ${JSON.stringify(expected)}, observed ${JSON.stringify(observed)}`);
  }

  retryBudget() {
    this.record('Resilience.RetryBudget', 'retry_budget', this.attempts, 3, this.attempts <= 3);
  }
}

setWorldConstructor(Probe);
Before({ name: 'Open scenario evidence' }, function (this: Probe, { testCaseStartedId }) {
  this.nativeId = testCaseStartedId;
  this.events.push('before-scenario');
});
// Intentionally declared before the seal hook: After hooks run in reverse order.
After({ name: 'Dispose scenario fixture' }, function (this: Probe) {
  this.events.push('fixture-cleanup');
  writeFileSync(join(process.env.PROBE_OUTPUT!, 'cucumber-js', `${this.invocation}.cleanup`), JSON.stringify(this.events));
});
After({ name: 'Seal scenario evidence' }, function (this: Probe) {
  this.events.push('after-scenario');
  const output = join(process.env.PROBE_OUTPUT!, 'cucumber-js');
  mkdirSync(output, { recursive: true });
  writeFileSync(join(output, `${this.invocation}.json`), JSON.stringify({
    runner: 'cucumber-js', nativeId: this.nativeId, invocation: this.invocation,
    case: this.mode, observer: this.observer, completed: true,
    events: this.events, assertions: this.assertions,
  }));
});
Given('the experiment case {string}', function (this: Probe, mode: string) {
  this.mode = mode;
  if (mode === 'setup-fail') throw new Error('Deliberate setup failure');
  this.observer = mode !== 'no-observer';
});
When('the public reader returns rows through the recording transport', function (this: Probe) {
  this.events.push('operation');
  this.attempts = this.mode === 'zero' ? 0 : this.mode === 'over-budget' ? 4 : 2;
  this.rows = this.mode === 'functional-fail' ? ['wrong'] : ['A', 'B'];
});
Then('the rows match the functional requirement', function (this: Probe) {
  if (this.mode !== 'missing-functional')
    this.record('Functional.RowsMatch', 'rows_match', this.rows, ['A', 'B'], this.rows.join(',') === 'A,B');
});
Then('the transport attempts meet the retry budget', function (this: Probe) {
  if (this.mode === 'missing-policy') return;
  if (this.mode === 'unreachable') {
    if (this.attempts < 0) this.retryBudget();
    return;
  }
  if (this.mode === 'misclassified') {
    this.record('Resilience.RetryBudget', 'rows_match', this.rows, ['A', 'B'], true);
    return;
  }
  this.retryBudget();
});
