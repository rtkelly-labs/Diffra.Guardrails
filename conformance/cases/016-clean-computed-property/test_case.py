class Rectangle:
    def __init__(self):
        self.width = 10
        self.height = 5

    @property
    def area(self) -> int:
        return self.width * self.height


def test_clean_computed_property_evaluates_simple_arithmetic():
    # Arrange
    rect = Rectangle()

    # Act
    area = rect.area

    # Assert
    assert area == 50, "area of ten by five rectangle is fifty"
