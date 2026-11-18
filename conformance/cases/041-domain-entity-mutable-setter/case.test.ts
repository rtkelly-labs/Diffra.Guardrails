import { expect, test } from "vitest";

export class OrderEntity {
  id = "";
  total = 0;
}

test("entity mutates total", () => {
  // Arrange
  const order = new OrderEntity();
  order.id = "ORD-1";
  order.total = 100;

  // Act
  order.total = 120;

  // Assert
  expect(order.total, "order total was mutated via setter").toBe(120);
});
