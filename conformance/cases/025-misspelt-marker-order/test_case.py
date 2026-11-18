from pricing import Basket


def test_marker_misspelt_triggers_diagnostic():
    # Arange
    basket = Basket(prices=[10, 20])

    # Act
    total = basket.total()

    # Assert
    assert total == 30, "sum of prices is thirty"
