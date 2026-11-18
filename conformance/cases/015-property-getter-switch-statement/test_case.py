class Order:
    def __init__(self):
        self.tier_code = 1

    @property
    def tier_description(self):
        if self.tier_code == 1:
            return "Standard"
        elif self.tier_code == 2:
            return "Premium"
        return "Unknown"


def test_property_getter_executes_switch_statement():
    # Arrange
    order = Order()

    # Act
    desc = order.tier_description

    # Assert
    assert desc == "Standard", "first tier is standard"
