import { expect, test } from "vitest";

test("lambda capturing outer threshold is allowed", () => {
  // Arrange
  const threshold = 15;
  const numbers = [10, 20, 30];

  // Act
  const filtered = numbers.filter((x) => x > threshold);

  // Assert
  expect(filtered.length, "two items exceed the dynamic threshold").toBe(2);
});
