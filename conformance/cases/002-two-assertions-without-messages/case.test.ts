import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("a discount code changes the total and the label", () => {
  // Arrange
  const basket = new Basket({ prices: [40, 60] });

  // Act
  const receipt = basket.checkout({ code: "SPRING15" });

  // Assert
  expect(receipt.total).toBe(85);
  expect(receipt.label).toBe("SPRING15 applied");
});
