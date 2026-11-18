import { expect, test } from "vitest";

test("status assigned across conditional branches", () => {
  // Arrange
  const discount = 20;

  // Act
  let tier: string;
  if (discount > 10) {
    tier = "Gold";
  } else {
    tier = "Standard";
  }

  // Assert
  expect(tier, "twenty percent discount qualifies for gold tier").toBe("Gold");
});
