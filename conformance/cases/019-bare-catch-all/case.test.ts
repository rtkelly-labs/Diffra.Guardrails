import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("bare catch block swallows fault", () => {
  // Arrange
  const basket = new Basket();

  // Act
  try {
    basket.applyCode("UNKNOWN");
  } catch {
    // Bare catch
  }

  // Assert
  expect(basket.total, "total remains zero when bare catch swallows error").toBe(0);
});
