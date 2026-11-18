def classify_status(code: int) -> str:
    if code < 0:
        return "Invalid"
    if code == 0:
        return "Pending"
    return "Active"


def test_early_return_flow_evaluates_purely():
    # Arrange
    code = 5

    # Act
    result = classify_status(code)

    # Assert
    assert result == "Active", "positive code classifies as active"
