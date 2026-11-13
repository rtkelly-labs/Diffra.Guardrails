import { expect, test } from "vitest";

test("read timestamp uses system clock", () => {
  // Arrange
  const baseline = 0;

  // Act
  const now = Date.now();

  // Assert
  expect(now, "system clock timestamp is after baseline").toBeGreaterThan(baseline);
});
