def test_switch_expression_binds_single_assignment():
    # Arrange
    status = 2

    # Act
    mapping = {1: "Active", 2: "Paused"}
    label = mapping.get(status, "Closed")

    # Assert
    assert label == "Paused", "status two corresponds to paused"
