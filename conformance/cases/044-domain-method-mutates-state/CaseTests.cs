using FluentAssertions;
using Xunit;

public class BankAccount
{
    private decimal _balance;

    public BankAccount(decimal initialBalance)
    {
        _balance = initialBalance;
    }

    public decimal Balance => _balance;

    public void Deposit(decimal amount)
    {
        _balance += amount;
    }
}

public class BankAccountTests
{
    [Fact]
    public void Account_deposits_amount()
    {
        // Arrange
        var account = new BankAccount(100m);

        // Act
        account.Deposit(50m);

        // Assert
        account.Balance.Should().Be(150m, "balance increases after deposit");
    }
}
