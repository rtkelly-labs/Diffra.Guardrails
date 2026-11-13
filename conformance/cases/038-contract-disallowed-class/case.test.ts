import { expect, test } from "vitest";

export class UserContract {
  constructor(public id: number, public name: string) {}
}

test("contract reads name", () => {
  // Arrange
  const user = new UserContract(1, "Alice");

  // Act
  const name = user.name;

  // Assert
  expect(name, "user name matches initialized value").toBe("Alice");
});
