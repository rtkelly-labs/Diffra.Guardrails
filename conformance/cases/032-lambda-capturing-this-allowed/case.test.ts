import { expect, test } from "vitest";

class PriceCalculator {
  private discountRate = 0.1;

  applyDiscount(prices: number[]): number[] {
    return prices.map((p) => p - p * this.discountRate);
  }
}

test("lambda capturing instance state is allowed", () => {
  // Arrange
  const calculator = new PriceCalculator();
  const prices = [100, 200];

  // Act
  const discounted = calculator.applyDiscount(prices);

  // Assert
  expect(discounted[0], "first price has ten percent discount deducted").toBe(90);
});
