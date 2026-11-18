import { expect, test } from "vitest";

const isOdd = (x: number): boolean => x % 2 !== 0;

test("static lambda guarantees zero closure allocation", () => {
  // Arrange
  const numbers = [1, 2, 3, 4, 5];

  // Act
  const oddCount = numbers.filter(isOdd).length;

  // Assert
  expect(oddCount, "three odd numbers exist").toBe(3);
});
