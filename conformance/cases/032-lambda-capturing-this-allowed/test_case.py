class PriceCalculator:
    def __init__(self):
        self._discount_rate = 0.1

    def apply_discount(self, prices):
        return [p - (p * self._discount_rate) for p in prices]


def test_lambda_capturing_instance_state_is_allowed():
    # Arrange
    calculator = PriceCalculator()
    prices = [100, 200]

    # Act
    discounted = calculator.apply_discount(prices)

    # Assert
    assert discounted[0] == 90, "first price has ten percent discount deducted"
