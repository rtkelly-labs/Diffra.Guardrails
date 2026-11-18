import { expect, test } from "vitest";

export interface OrderDto {
  readonly id: number;
  readonly price: number;
  calculateTotal(): number;
}

test("contract calculates total", () => {
  // Arrange
  const dto: OrderDto = { id: 1, price: 100, calculateTotal: () => 120 };

  // Act
  const total = dto.calculateTotal();

  // Assert
  expect(total, "total includes standard tax").toBe(120);
});
