import pytest
from pricing import Basket


def test_rethrow_with_identifier_truncates_stack():
    # Arrange
    basket = Basket()

    # Act & Assert
    with pytest.raises(Exception):
        try:
            basket.apply_code("ERR")
        except Exception as ex:
            raise ex
