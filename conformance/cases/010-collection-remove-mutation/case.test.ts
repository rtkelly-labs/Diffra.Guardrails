import { expect, test } from "vitest";

test("remove mutation deletes item in place", () => {
  // Arrange
  const items = ["item1", "item2"];

  // Act
  items.splice(0, 1);

  // Assert
  expect(items.length, "one item remains after removal").toBe(1);
});
