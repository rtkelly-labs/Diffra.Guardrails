import { expect, test } from "vitest";

test("collection expression combines purely", () => {
  // Arrange
  const first = [1, 2];
  const second = [3, 4];

  // Act
  const combined = [...first, ...second];

  // Assert
  expect(combined.length, "combining two length two arrays produces length four array").toBe(4);
});
