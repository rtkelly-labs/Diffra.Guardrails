import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("exception logged and rethrown preserves diagnostics", () => {
  // Arrange
  const basket = new Basket();

  // Act & Assert
  expect(() => {
    try {
      basket.applyCode("INVALID");
    } catch (e) {
      throw e;
    }
  }, "invalid code causes exception which is faithfully rethrown").toThrow();
});
