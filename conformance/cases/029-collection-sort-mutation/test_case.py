def test_in_place_sort_mutates_collection():
    # Arrange
    scores = [30, 10, 20]

    # Act
    scores.sort()

    # Assert
    assert scores[0] == 10, "lowest score is first after in-place sort"
