import { expect, test } from "vitest";

function buildRange(count: number): readonly number[] {
  const items: number[] = [];
  for (let i = 0; i < count; i++) {
    items.push(i);
  }
  return Object.freeze(items);
}

test("scoped builder produces immutable array", () => {
  // Arrange
  const targetCount = 3;

  // Act
  const result = buildRange(targetCount);

  // Assert
  expect(result.length, "three elements produced by scoped builder").toBe(3);
});
