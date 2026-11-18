class Basket:
    def __init__(self):
        self.items = [10, 25]

    @property
    def computed_total(self):
        total = 0
        for price in self.items:
            total += price
        return total


def test_property_getter_calculates_sum_via_loop():
    # Arrange
    basket = Basket()

    # Act
    total = basket.computed_total

    # Assert
    assert total == 35, "sum of items should equal thirty five"
