class UserContract:
    def __init__(self, id: int, name: str):
        self.id = id
        self.name = name

def test_contract_reads_name():
    # Arrange
    user = UserContract(1, "Alice")

    # Act
    name = user.name

    # Assert
    assert name == "Alice", "user name matches initialized value"
