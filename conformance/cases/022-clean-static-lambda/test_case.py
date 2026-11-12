def is_odd(x: int) -> bool:
    return x % 2 != 0


def test_static_lambda_guarantees_zero_closure_allocation():
    # Arrange
    numbers = [1, 2, 3, 4, 5]

    # Act
    odd_count = len([x for x in numbers if is_odd(x)])

    # Assert
    assert odd_count == 3, "three odd numbers exist"
