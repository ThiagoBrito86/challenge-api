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
            .HasMaxLength(200)
            .HasColumnName("ExecutedService"); 

        builder.Property(x => x.Date)
            .IsRequired()
            .HasColumnName("Date"); 

        builder.Property(x => x.Responsible)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Responsible");

        builder.Property(x => x.City)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("City");

        builder.Property(x => x.ProcessingTime)
        .IsRequired()
        .HasColumnName("ProcessingTime")
        .HasConversion(
            v => (int)((DateTimeOffset)v).ToUnixTimeSeconds(), // DateTime → INT
            v => DateTimeOffset.FromUnixTimeSeconds(v).DateTime // INT → DateTime
        );

        builder.OwnsOne(x => x.WeatherData, weatherData =>
        {            
            weatherData.Property(w => w.Description)
                .HasColumnName("Description") 
                .HasMaxLength(100)
                .IsRequired(true); 

            weatherData.Property(w => w.Humidity)
                .HasColumnName("Humidity") 
                .IsRequired(true); 

            weatherData.Property(w => w.Pressure)
                .HasColumnName("Pressure") 
                .IsRequired(true); 

            weatherData.Property(w => w.Timestamp)
                .HasColumnName("Timestamp") 
                .IsRequired(true); 

            weatherData.OwnsOne(w => w.Temperature, temperature =>
            {
                temperature.Property(t => t.Value)
                    .HasColumnName("Temperature") 
                    .HasPrecision(5, 2)
                    .IsRequired(true);

                temperature.Property(t => t.Condition)
                    .HasColumnName("WeatherCondition") 
                    .HasConversion<int>()
                    .IsRequired(true);
            });
        });

        // Ignorar Domain Events (não persistir)
        builder.Ignore(x => x.DomainEvents);
    }
}