import { expect, test } from "vitest";

test("switch statement mutates procedural variable", () => {
  // Arrange
  const status = 2;

  // Act
  let label: string;
  switch (status) {
    case 1:
      label = "Active";
      break;
    case 2:
      label = "Paused";
      break;
    default:
      label = "Closed";
      break;
  }

  // Assert
  expect(label, "status two corresponds to paused").toBe("Paused");
});
