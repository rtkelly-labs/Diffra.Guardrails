@remote_io @requirement_read_rows
Feature: Read rows with an explicit request budget
  The public result is [A, B]. At most three transport attempts may start.
  A completed observer is required, including for an intentional cache hit.

  Scenario Outline: Read rows under <case>
    Given the experiment case "<case>"
    When the public reader returns rows through the recording transport
    Then the rows match the functional requirement
    And the transport attempts meet the retry budget

    Examples:
      | case               |
      | good               |
      | missing-functional |
      | missing-policy     |
      | misclassified      |
      | unreachable        |
      | no-observer        |
      | over-budget        |
      | functional-fail    |
      | setup-fail         |
      | zero               |
