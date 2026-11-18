import { expect, test } from "vitest";

test("add mutation appends item in place", () => {
  // Arrange
  const items = ["item1"];

  // Act
  items.push("item2");

  // Assert
  expect(items.length, "two items should be present after addition").toBe(2);
});
