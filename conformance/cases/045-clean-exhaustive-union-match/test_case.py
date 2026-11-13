from enum import Enum

class OrderStatus(Enum):
    PENDING = 1
    APPROVED = 2
    CANCELLED = 3

def test_switch_evaluates_discount_exhaustively():
    # Arrange
    status = OrderStatus.APPROVED

    # Act
    match status:
        case OrderStatus.PENDING:
            discount = 0.0
        case OrderStatus.APPROVED:
            discount = 0.05
        case OrderStatus.CANCELLED:
            discount = 0.0

    # Assert
    assert discount == 0.05, "approved order receives five percent discount"
