from dataclasses import dataclass
from typing import Sequence

@dataclass(frozen=True)
class ProductDto:
    id: int
    sku: str
    tags: Sequence[str]

def test_contract_reads_sku_cleanly():
    # Arrange
    product = ProductDto(1, "SKU-100", ("hardware", "tools"))

    # Act
    sku = product.sku

    # Assert
    assert sku == "SKU-100", "product sku matches initialized value"
