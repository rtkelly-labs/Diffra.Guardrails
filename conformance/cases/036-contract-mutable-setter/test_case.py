from dataclasses import dataclass

@dataclass
class InvoiceDto:
    id: int
    amount: float

def test_contract_sets_amount():
    # Arrange
    invoice = InvoiceDto(1, 50.0)

    # Act
    invoice.amount = 75.0

    # Assert
    assert invoice.amount == 75.0, "amount was mutated via set"
