import { expect, test } from "vitest";

export type OrderStatus = "Pending" | "Approved" | "Cancelled";

test("switch evaluates discount exhaustively", () => {
  // Arrange
  const status: OrderStatus = "Approved";

  // Act
  let discount: number;
  switch (status) {
    case "Pending":
      discount = 0.0;
      break;
    case "Approved":
      discount = 0.05;
      break;
    case "Cancelled":
      discount = 0.0;
      break;
  }

  // Assert
  expect(discount, "approved order receives five percent discount").toBe(0.05);
});
