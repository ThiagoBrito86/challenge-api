using ServiceControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceControl.Infrastructure.Persistence.Configurations;

public class WorkRecordConfiguration : IEntityTypeConfiguration<WorkRecord>
{
    public void Configure(EntityTypeBuilder<WorkRecord> builder)
    {
        builder.ToTable("WorkRecords");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ExecutedService)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Date)
            .IsRequired();

        builder.Property(x => x.Responsible)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProcessingTime)
            .IsRequired();

        // Value Object WeatherData como JSON
        builder.OwnsOne(x => x.WeatherData, weatherData =>
        {
            weatherData.Property(w => w.Description)
                .HasMaxLength(100);

            weatherData.Property(w => w.Humidity);
            weatherData.Property(w => w.Pressure);
            weatherData.Property(w => w.Timestamp);

            // Value Object Temperature
            weatherData.OwnsOne(w => w.Temperature, temperature =>
            {
                temperature.Property(t => t.Value)
                    .HasColumnName("Temperature")
                    .HasPrecision(5, 2);

                temperature.Property(t => t.Condition)
                    .HasColumnName("WeatherCondition")
                    .HasConversion<int>();
            });
        });

        // (não persistir)
        builder.Ignore(x => x.DomainEvents);
    }
}