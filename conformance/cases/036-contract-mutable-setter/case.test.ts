import { expect, test } from "vitest";

export interface InvoiceDto {
  readonly id: number;
  amount: number;
}

test("contract sets amount", () => {
  // Arrange
  const invoice: InvoiceDto = { id: 1, amount: 50 };

  // Act
  invoice.amount = 75;

  // Assert
  expect(invoice.amount, "amount was mutated via set").toBe(75);
});
