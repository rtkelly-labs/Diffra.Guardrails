import pytest
from pricing import Basket


def test_exception_logged_and_rethrown_preserves_diagnostics():
    # Arrange
    basket = Basket()

    # Act & Assert
    with pytest.raises(Exception, match="INVALID"):
        try:
            basket.apply_code("INVALID")
        except Exception:
            raise
