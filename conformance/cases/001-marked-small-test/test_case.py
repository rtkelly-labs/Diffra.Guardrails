from pricing import Basket


def test_a_percentage_discount_is_taken_off_the_total():
    # Arrange
    basket = Basket(prices=[40, 60])

    # Act
    total = basket.total(discount_percent=15)

    # Assert
    assert total == 85, "the discount is taken off the price, not added to it"
