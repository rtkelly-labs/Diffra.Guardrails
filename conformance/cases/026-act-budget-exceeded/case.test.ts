import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("act block has two statements", () => {
  // Arrange
  const basket = new Basket();

  // Act
  basket.applyDiscount(10);
  const total = basket.total();

  // Assert
  expect(total, "ten percent discount is deducted").toBe(90);
});
