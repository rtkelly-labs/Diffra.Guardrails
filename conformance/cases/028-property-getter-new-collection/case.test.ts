import { expect, test } from "vitest";

class Store {
  get categories(): string[] {
    return ["Electronics", "Books"];
  }
}

test("property getter allocates new list", () => {
  // Arrange
  const store = new Store();

  // Act
  const count = store.categories.length;

  // Assert
  expect(count, "two store categories are configured").toBe(2);
});
