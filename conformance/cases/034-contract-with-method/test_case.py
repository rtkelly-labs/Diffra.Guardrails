from dataclasses import dataclass

@dataclass(frozen=True)
class OrderDto:
    id: int
    price: float

    def calculate_total(self) -> float:
        return self.price * 1.2

def test_contract_calculates_total():
    # Arrange
    dto = OrderDto(1, 100.0)

    # Act
    total = dto.calculate_total()

    # Assert
    assert total == 120.0, "total includes standard tax"
