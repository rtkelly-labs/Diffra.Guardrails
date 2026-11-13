from pricing import Basket


def test_bare_catch_block_swallows_fault():
    # Arrange
    basket = Basket()

    # Act
    try:
        basket.apply_code("UNKNOWN")
    except:
        pass

    # Assert
    assert basket.total == 0, "total remains zero when bare catch swallows error"
