def test_status_assigned_across_conditional_branches():
    # Arrange
    discount = 20

    # Act
    tier = None
    if discount > 10:
        tier = "Gold"
    else:
        tier = "Standard"

    # Assert
    assert tier == "Gold", "twenty percent discount qualifies for gold tier"
