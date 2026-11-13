class BankAccount:
    def __init__(self, initial_balance: float):
        self.balance = initial_balance

    def deposit(self, amount: float) -> None:
        self.balance += amount

def test_account_deposits_amount():
    # Arrange
    account = BankAccount(100.0)

    # Act
    account.deposit(50.0)

    # Assert
    assert account.balance == 150.0, "balance increases after deposit"
