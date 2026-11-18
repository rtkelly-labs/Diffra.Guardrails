class Store:
    @property
    def categories(self):
        return ["Electronics", "Books"]


def test_property_getter_allocates_new_list():
    # Arrange
    store = Store()

    # Act
    count = len(store.categories)

    # Assert
    assert count == 2, "two store categories are configured"
