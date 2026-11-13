import { expect, test } from "vitest";

export type OrderStatus = "Pending" | "Approved" | "Cancelled";

test("switch evaluates discount with discard", () => {
  // Arrange
  const status: OrderStatus = "Pending";

  // Act
  let discount: number;
  switch (status) {
    case "Pending":
      discount = 0.0;
      break;
    case "Approved":
      discount = 0.05;
      break;
    default:
      discount = 0.0;
  }

  // Assert
  expect(discount, "pending order receives zero discount").toBe(0.0);
});
