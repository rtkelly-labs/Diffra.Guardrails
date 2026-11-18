import { expect, test } from "vitest";

class Rectangle {
  width = 10;
  height = 5;

  get area(): number {
    return this.width * this.height;
  }
}

test("clean computed property evaluates simple arithmetic", () => {
  // Arrange
  const rect = new Rectangle();

  // Act
  const area = rect.area;

  // Assert
  expect(area, "area of ten by five rectangle is fifty").toBe(50);
});
