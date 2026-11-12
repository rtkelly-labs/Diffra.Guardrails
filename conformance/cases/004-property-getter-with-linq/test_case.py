class Order:
    def __init__(self):
        self.items = [10, 20, 30]

    @property
    def discounted_items(self):
        return [x for x in self.items if x > 15]


def test_property_getter_evaluates_active_items():
    # Arrange
    order = Order()

    # Act
    count = len(order.discounted_items)

    # Assert
    assert count == 2, "only items over fifteen qualify for discount"
