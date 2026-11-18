import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("marker misspelt triggers diagnostic", () => {
  // Arange
  const basket = new Basket([10, 20]);

  // Act
  const total = basket.total();

  // Assert
  expect(total, "sum of prices is thirty").toBe(30);
});
