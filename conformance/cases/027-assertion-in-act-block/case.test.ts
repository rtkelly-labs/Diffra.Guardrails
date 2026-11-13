import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("assertion placed inside act block", () => {
  // Arrange
  const basket = new Basket([20]);

  // Act
  const total = basket.total();
  expect(total, "basket total equals item price").toBe(20);

  // Assert
});
