import { expect, test } from "vitest";

export class BankAccount {
  balance: number;

  constructor(initialBalance: number) {
    this.balance = initialBalance;
  }

  deposit(amount: number): void {
    this.balance += amount;
  }
}

test("account deposits amount", () => {
  // Arrange
  const account = new BankAccount(100);

  // Act
  account.deposit(50);

  // Assert
  expect(account.balance, "balance increases after deposit").toBe(150);
});
