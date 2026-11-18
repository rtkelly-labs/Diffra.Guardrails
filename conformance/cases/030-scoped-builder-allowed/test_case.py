def build_range(count: int):
    items = []
    for i in range(count):
        items.append(i)
    return tuple(items)


def test_scoped_builder_produces_immutable_array():
    # Arrange
    target_count = 3

    # Act
    result = build_range(target_count)

    # Assert
    assert len(result) == 3, "three elements produced by scoped builder"
