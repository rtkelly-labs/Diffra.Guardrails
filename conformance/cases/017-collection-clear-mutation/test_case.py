def test_clear_mutation_empties_collection_in_place():
    # Arrange
    items = ["item1", "item2"]

    # Act
    items.clear()

    # Assert
    assert len(items) == 0, "collection is empty after clear"
