def test_remove_mutation_deletes_item_in_place():
    # Arrange
    items = ["item1", "item2"]

    # Act
    items.remove("item1")

    # Assert
    assert len(items) == 1, "one item remains after removal"
