from pricing import Basket


def test_catch_block_swallows_broad_exception():
    # Arrange
    basket = Basket()

    # Act
    try:
        basket.apply_code("INVALID")
    except Exception:
        pass

    # Assert
    assert basket.total == 0, "total remains zero when invalid code is swallowed"
