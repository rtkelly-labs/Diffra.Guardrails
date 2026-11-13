def test_switch_statement_mutates_procedural_variable():
    # Arrange
    status = 2

    # Act
    label = None
    if status == 1:
        label = "Active"
    elif status == 2:
        label = "Paused"
    else:
        label = "Closed"

    # Assert
    assert label == "Paused", "status two corresponds to paused"
