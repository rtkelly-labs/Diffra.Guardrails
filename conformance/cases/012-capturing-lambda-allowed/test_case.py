def test_lambda_capturing_outer_threshold_is_allowed():
    # Arrange
    threshold = 15
    numbers = [10, 20, 30]

    # Act
    filtered = list(filter(lambda x: x > threshold, numbers))

    # Assert
    assert len(filtered) == 2, "two items exceed the dynamic threshold"
