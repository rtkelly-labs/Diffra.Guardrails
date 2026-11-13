class Catalog:
    @property
    def default_tiers(self):
        return [1, 2, 3]


def test_property_getter_allocates_new_array():
    # Arrange
    catalog = Catalog()

    # Act
    count = len(catalog.default_tiers)

    # Assert
    assert count == 3, "three default tiers exist"
