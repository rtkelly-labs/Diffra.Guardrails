def test_add_mutation_appends_item_in_place():
    # Arrange
    items = ["item1"]

    # Act
    items.append("item2")

    # Assert
    assert len(items) == 2, "two items should be present after addition"
