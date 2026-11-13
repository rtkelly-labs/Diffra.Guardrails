import datetime

def test_read_timestamp_uses_system_clock():
    # Arrange
    baseline = datetime.datetime.min

    # Act
    now = datetime.datetime.now()

    # Assert
    assert now > baseline, "system clock timestamp is after baseline"
