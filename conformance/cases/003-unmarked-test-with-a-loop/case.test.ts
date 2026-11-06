import { expect, test } from "vitest";
import { Basket } from "./pricing";

test("no discount makes the total negative", () => {
  const basket = new Basket({ prices: [40, 60] });
  for (const percent of [0, 50, 100, 150]) {
    const total = basket.total({ discountPercent: percent });
    expect(total, "a discount can bring the total to zero but never below it").toBeGreaterThanOrEqual(0);
  }
});
