import { expect, test } from "vitest";

class Order {
  items: number[] = [10, 20, 30];
  get discountedItems(): number[] {
    return this.items.filter((x) => x > 15);
  }
}

test("property getter evaluates active items", () => {
  // Arrange
  const order = new Order();

  // Act
  const count = order.discountedItems.length;

  // Assert
  expect(count, "only items over fifteen qualify for discount").toBe(2);
});
