import { expect, test } from "vitest";

test("filter uses non static pure lambda", () => {
  // Arrange
  const numbers = [1, 2, 3, 4];

  // Act
  const evens = numbers.filter((x) => x % 2 === 0);

  // Assert
  expect(evens.length, "two even numbers should be matched").toBe(2);
});
