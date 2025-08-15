using ServiceControl.Domain.Enums;
using ServiceControl.Domain.Exceptions;
using ServiceControl.Domain.ValueObjects;


namespace ServiceControl.UnitTests.Domain.ValueObjects;

public class TemperatureTests
{
    [Theory]
    [InlineData(15, WeatherCondition.ExcellentConditions)]
    [InlineData(20, WeatherCondition.ExcellentConditions)]
    [InlineData(30, WeatherCondition.ExcellentConditions)]
    [InlineData(10, WeatherCondition.Pleasant)]
    [InlineData(12, WeatherCondition.Pleasant)]
    [InlineData(14, WeatherCondition.Pleasant)]
    [InlineData(5, WeatherCondition.Impracticable)]
    [InlineData(35, WeatherCondition.Impracticable)]
    public void Constructor_ValidTemperature_ShouldClassifyCorrectly(decimal value, WeatherCondition expectedCondition)
    {
        // Act
        var temperature = new Temperature(value);

        // Assert
        Assert.Equal(value, temperature.Value);
        Assert.Equal(expectedCondition, temperature.Condition);
    }

    [Theory]
    [InlineData(-60)]
    [InlineData(70)]
    public void Constructor_InvalidTemperature_ShouldThrowException(decimal value)
    {
        // Act & Assert
        Assert.Throws<InvalidTemperatureException>(() => new Temperature(value));
    }

    [Theory]
    [InlineData(20, true)]
    [InlineData(12, true)]
    [InlineData(5, false)]
    [InlineData(35, false)]
    public void CanExecuteWork_ShouldReturnCorrectValue(decimal value, bool expected)
    {
        // Arrange
        var temperature = new Temperature(value);

        // Act
        var result = temperature.CanExecuteWork();

        // Assert
        Assert.Equal(expected, result);
    }
}