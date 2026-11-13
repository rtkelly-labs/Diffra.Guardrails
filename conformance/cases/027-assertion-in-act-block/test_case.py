from pricing import Basket


def test_assertion_placed_inside_act_block():
    # Arrange
    basket = Basket(prices=[20])

    # Act
    total = basket.total()
    assert total == 20, "basket total equals item price"

    # Assert
