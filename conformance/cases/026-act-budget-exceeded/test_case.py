from pricing import Basket


def test_act_block_has_two_statements():
    # Arrange
    basket = Basket()

    # Act
    basket.apply_discount(10)
    total = basket.total()

    # Assert
    assert total == 90, "ten percent discount is deducted"
