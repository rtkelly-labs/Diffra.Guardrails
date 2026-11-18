from pricing import Basket


def test_filtered_exception_catch_is_allowed():
    # Arrange
    basket = Basket()
    was_caught = False

    # Act
    try:
        basket.apply_code("EXPIRED")
    except ValueError as ex:
        if "EXPIRED" in str(ex):
            was_caught = True
        else:
            raise

    # Assert
    assert was_caught is True, "expired code exception should match filter"
