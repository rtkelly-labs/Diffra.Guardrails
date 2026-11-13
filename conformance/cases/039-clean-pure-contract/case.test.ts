import { expect, test } from "vitest";

export interface ProductDto {
  readonly id: number;
  readonly sku: string;
  readonly tags: readonly string[];
}

test("contract reads sku cleanly", () => {
  // Arrange
  const product: ProductDto = { id: 1, sku: "SKU-100", tags: ["hardware", "tools"] };

  // Act
  const sku = product.sku;

  // Assert
  expect(sku, "product sku matches initialized value").toBe("SKU-100");
});
