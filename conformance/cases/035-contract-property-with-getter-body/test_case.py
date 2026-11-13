from dataclasses import dataclass

@dataclass(frozen=True)
class CustomerDto:
    first_name: str
    last_name: str

    @property
    def full_name(self) -> str:
        return f"{self.first_name} {self.last_name}"

def test_contract_formats_full_name():
    # Arrange
    customer = CustomerDto("Ada", "Lovelace")

    # Act
    name = customer.full_name

    # Assert
    assert name == "Ada Lovelace", "full name joins first and last"
