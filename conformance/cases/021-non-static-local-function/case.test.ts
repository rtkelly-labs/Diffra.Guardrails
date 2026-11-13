import { expect, test } from "vitest";

test("local function without static captures nothing", () => {
  // Arrange
  const numbers = [2, 4, 6];
  function halve(x: number): number {
    return x / 2;
  }

  // Act
  const results = numbers.map(halve);

  // Assert
  expect(results, "halving even numbers produces one, two, three").toEqual([1, 2, 3]);
});
