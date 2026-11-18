import { expect, test } from "vitest";

class Order {
  tierCode = 1;

  get tierDescription(): string {
    switch (this.tierCode) {
      case 1:
        return "Standard";
      case 2:
        return "Premium";
      default:
        return "Unknown";
    }
  }
}

test("property getter executes switch statement", () => {
  // Arrange
  const order = new Order();

  // Act
  const desc = order.tierDescription;

  // Assert
  expect(desc, "first tier is standard").toBe("Standard");
});
