def test_local_function_without_static_captures_nothing():
    # Arrange
    numbers = [2, 4, 6]

    def halve(x):
        return x // 2

    # Act
    results = [halve(x) for x in numbers]

    # Assert
    assert results == [1, 2, 3], "halving even numbers produces one, two, three"
