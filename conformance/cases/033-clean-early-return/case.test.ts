import { expect, test } from "vitest";

function classifyStatus(code: number): string {
  if (code < 0) {
    return "Invalid";
  }
  if (code === 0) {
    return "Pending";
  }
  return "Active";
}

test("early return flow evaluates purely", () => {
  // Arrange
  const code = 5;

  // Act
  const result = classifyStatus(code);

  // Assert
  expect(result, "positive code classifies as active").toBe("Active");
});
