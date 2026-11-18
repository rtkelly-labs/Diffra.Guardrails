import { expect, test } from "vitest";

export interface TeamDto {
  readonly id: number;
  members: string[];
}

test("contract reads members", () => {
  // Arrange
  const team: TeamDto = { id: 1, members: ["Alice", "Bob"] };

  // Act
  const count = team.members.length;

  // Assert
  expect(count, "two members present in team").toBe(2);
});
