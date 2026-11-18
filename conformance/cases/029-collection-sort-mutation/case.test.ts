import { expect, test } from "vitest";

test("in place sort mutates collection", () => {
  // Arrange
  const scores = [30, 10, 20];

  // Act
  scores.sort((a, b) => a - b);

  // Assert
  expect(scores[0], "lowest score is first after in-place sort").toBe(10);
});
