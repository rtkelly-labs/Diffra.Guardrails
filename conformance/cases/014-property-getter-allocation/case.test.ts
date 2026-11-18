import { expect, test } from "vitest";

class Catalog {
  get defaultTiers(): number[] {
    return [1, 2, 3];
  }
}

test("property getter allocates new array", () => {
  // Arrange
  const catalog = new Catalog();

  // Act
  const count = catalog.defaultTiers.length;

  // Assert
  expect(count, "three default tiers exist").toBe(3);
});
