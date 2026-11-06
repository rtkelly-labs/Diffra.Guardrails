import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("a percentage discount is taken off the total", () => {
  // Arrange
  const basket = new Basket({ prices: [40, 60] });

  // Act
  const total = basket.total({ discountPercent: 15 });

  // Assert
  expect(total, "the discount is taken off the price, not added to it").toBe(85);
});
