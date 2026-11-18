import { expect, test } from "vitest";

export class MutableAddress {
  street = "";
}

export interface Customer {
  readonly id: string;
  readonly address: MutableAddress;
}

test("record reads address street", () => {
  // Arrange
  const addr = new MutableAddress();
  addr.street = "Main St";
  const customer: Customer = { id: "C-1", address: addr };

  // Act
  const street = customer.address.street;

  // Assert
  expect(street, "street matches customer address").toBe("Main St");
});
