from dataclasses import dataclass

class MutableAddress:
    def __init__(self, street: str):
        self.street = street

@dataclass(frozen=True)
class Customer:
    id: str
    address: MutableAddress

def test_record_reads_address_street():
    # Arrange
    customer = Customer("C-1", MutableAddress("Main St"))

    # Act
    street = customer.address.street

    # Assert
    assert street == "Main St", "street matches customer address"
