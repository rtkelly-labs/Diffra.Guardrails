def test_filter_uses_non_static_pure_lambda():
    # Arrange
    numbers = [1, 2, 3, 4]

    # Act
    evens = list(filter(lambda x: x % 2 == 0, numbers))

    # Assert
    assert len(evens) == 2, "two even numbers should be matched"
