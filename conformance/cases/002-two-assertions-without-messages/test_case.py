from pricing import Basket


def test_a_discount_code_changes_the_total_and_the_label():
    # Arrange
    basket = Basket(prices=[40, 60])

    # Act
    receipt = basket.checkout(code="SPRING15")

    # Assert
    assert receipt.total == 85
    assert receipt.label == "SPRING15 applied"
