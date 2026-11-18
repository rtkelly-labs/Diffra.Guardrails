class OrderEntity:
    def __init__(self, order_id: str, total: float):
        self.order_id = order_id
        self.total = total

def test_entity_mutates_total():
    # Arrange
    order = OrderEntity("ORD-1", 100.0)

    # Act
    order.total = 120.0

    # Assert
    assert order.total == 120.0, "order total was mutated via setter"
