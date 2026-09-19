using Domain.ValueObjects;
using FluentAssertions;

namespace Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_DefaultsCurrencyToUsd()
    {
        var money = Money.Of(100m);

        money.Amount.Should().Be(100m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Of_UppercasesCurrency()
    {
        var money = Money.Of(50m, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Of_WithNegativeAmount_Throws()
    {
        var act = () => Money.Of(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Of_WithBlankCurrency_Throws(string currency)
    {
        var act = () => Money.Of(10m, currency);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_SameCurrency_SumsAmount()
    {
        var result = Money.Of(10m).Add(Money.Of(5m));

        result.Amount.Should().Be(15m);
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var act = () => Money.Of(10m, "USD").Add(Money.Of(5m, "EUR"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_ReducesAmount()
    {
        var result = Money.Of(10m).Subtract(Money.Of(3m));

        result.Amount.Should().Be(7m);
    }

    [Fact]
    public void Subtract_BelowZero_ClampsToZero()
    {
        var result = Money.Of(3m).Subtract(Money.Of(10m));

        result.Amount.Should().Be(0m);
    }

    [Fact]
    public void ApplyPercentageDiscount_ReducesByPercentage()
    {
        var result = Money.Of(100m).ApplyPercentageDiscount(20m);

        result.Amount.Should().Be(80m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ApplyPercentageDiscount_OutOfRange_Throws(decimal percentage)
    {
        var act = () => Money.Of(100m).ApplyPercentageDiscount(percentage);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var a = Money.Of(10m, "USD");
        var b = Money.Of(10m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentAmount_AreNotEqual()
    {
        Money.Of(10m).Should().NotBe(Money.Of(11m));
    }

    [Fact]
    public void ToString_FormatsAmountWithTwoDecimalsAndCurrency()
    {
        Money.Of(1234.5m).ToString().Should().Be("1234.50 USD");
    }
}