from enum import Enum

class OrderStatus(Enum):
    PENDING = 1
    APPROVED = 2
    CANCELLED = 3

def test_switch_evaluates_discount_with_discard():
    # Arrange
    status = OrderStatus.PENDING

    # Act
    match status:
        case OrderStatus.PENDING:
            discount = 0.0
        case OrderStatus.APPROVED:
            discount = 0.05
        case _:
            discount = 0.0

    # Assert
    assert discount == 0.0, "pending order receives zero discount"
