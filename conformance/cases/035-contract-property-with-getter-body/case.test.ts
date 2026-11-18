import { expect, test } from "vitest";

export class CustomerDto {
  constructor(public readonly firstName: string, public readonly lastName: string) {}

  get fullName(): string {
    return `${this.firstName} ${this.lastName}`;
  }
}

test("contract formats full name", () => {
  // Arrange
  const customer = new CustomerDto("Ada", "Lovelace");

  // Act
  const name = customer.fullName;

  // Assert
  expect(name, "full name joins first and last").toBe("Ada Lovelace");
});
