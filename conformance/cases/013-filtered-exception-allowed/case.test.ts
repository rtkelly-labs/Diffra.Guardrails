import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("filtered exception catch is allowed", () => {
  // Arrange
  const basket = new Basket();
  let wasCaught = false;

  // Act
  try {
    basket.applyCode("EXPIRED");
  } catch (ex: any) {
    if (ex?.message?.includes("EXPIRED")) {
      wasCaught = true;
    } else {
      throw ex;
    }
  }

  // Assert
  expect(wasCaught, "expired code exception should match filter").toBe(true);
});
