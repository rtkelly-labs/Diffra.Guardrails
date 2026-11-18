import { expect, test } from "vitest";

class Basket {
  items: number[] = [10, 25];

  get computedTotal(): number {
    let sum = 0;
    for (const price of this.items) {
      sum += price;
    }
    return sum;
  }
}

test("property getter calculates sum via loop", () => {
  // Arrange
  const basket = new Basket();

  // Act
  const total = basket.computedTotal;

  // Assert
  expect(total, "sum of items should equal thirty five").toBe(35);
});
