import { expect, test } from "vitest";

class Order {
  constructor(readonly items: readonly number[]) {}

  get itemCount(): number {
    return this.items.length;
  }
}

test("clean functional pipeline executes purely", () => {
  // Arrange
  const initial = new Order([10, 20]);
  const nextItem = 30;

  // Act
  const updated = new Order([...initial.items, nextItem]);

  // Assert
  expect(
    updated.items.filter((x) => x > 15).length,
    "two items exceed fifteen in the pure pipeline",
  ).toBe(2);
});
