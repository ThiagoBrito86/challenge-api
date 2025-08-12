using ServiceControl.Domain.Enums;

namespace ServiceControl.Domain.ValueObjects;

public record Temperature
{
    public decimal Value { get; }
    public WeatherCondition Condition { get; }

    public Temperature(decimal value)
    {
        if (value < -50 || value > 60)
            throw new InvalidTemperatureException("A temperatura deve estar entre -50°C and 60°C");

        Value = value;
        Condition = ClassifyCondition(value);
    }

    private static WeatherCondition ClassifyCondition(decimal temperature)
    {
        return temperature switch
        {
            >= 15 and <= 30 => WeatherCondition.ExcellentConditions,
            >= 10 and <= 14 => WeatherCondition.Pleasant,
            _ => WeatherCondition.Impracticable
        };
    }

    public bool CanExecuteWork() => Condition != WeatherCondition.Impracticable;

    public string GetConditionDescription() => Condition switch
    {
        WeatherCondition.ExcellentConditions => "ótimas condições",
        WeatherCondition.Pleasant => "agradável",
        WeatherCondition.Impracticable => "impraticável",
        _ => "indefinido"
    };
}