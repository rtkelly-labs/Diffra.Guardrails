def test_collection_expression_combines_purely():
    # Arrange
    first = [1, 2]
    second = [3, 4]

    # Act
    combined = [*first, *second]

    # Assert
    assert len(combined) == 4, "combining two length two arrays produces length four array"
