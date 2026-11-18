import { expect, test } from "vitest";

test("clear mutation empties collection in place", () => {
  // Arrange
  const items = ["item1", "item2"];

  // Act
  items.length = 0;

  // Assert
  expect(items.length, "collection is empty after clear").toBe(0);
});
