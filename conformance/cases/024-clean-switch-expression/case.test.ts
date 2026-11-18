import { expect, test } from "vitest";

test("switch expression binds single assignment", () => {
  // Arrange
  const status = 2;

  // Act
  const label =
    status === 1 ? "Active" : status === 2 ? "Paused" : "Closed";

  // Assert
  expect(label, "status two corresponds to paused").toBe("Paused");
});
