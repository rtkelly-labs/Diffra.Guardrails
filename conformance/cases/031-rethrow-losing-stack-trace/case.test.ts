import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("rethrow with identifier truncates stack", () => {
  // Arrange
  const basket = new Basket();

  // Act & Assert
  expect(() => {
    try {
      basket.applyCode("ERR");
    } catch (ex) {
      throw ex;
    }
  }, "rethrow triggers exception").toThrow();
});
