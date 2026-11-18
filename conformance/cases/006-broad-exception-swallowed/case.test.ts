import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("catch block swallows broad exception", () => {
  // Arrange
  const basket = new Basket();

  // Act
  try {
    basket.applyCode("INVALID");
  } catch (e) {
    // Swallowed
  }

  // Assert
  expect(basket.total, "total remains zero when invalid code is swallowed").toBe(0);
});
