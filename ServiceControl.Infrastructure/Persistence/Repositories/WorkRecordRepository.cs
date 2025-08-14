using Microsoft.Extensions.Logging;
using ServiceControl.Domain.Entities;
using ServiceControl.Domain.Intefaces.Repositories;
using ServiceControl.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using ServiceControl.Infrastructure.Services.Resilience;

namespace ServiceControl.Infrastructure.Persistence.Repositories;

public class WorkRecordRepository : IWorkRecordRepository
{
    private readonly WorkRecordContext _context;
    private readonly IRetryPolicy _retryPolicy;
    private readonly ILogger<WorkRecordRepository> _logger;

    public WorkRecordRepository(
        WorkRecordContext context,
        IRetryPolicy retryPolicy,
        ILogger<WorkRecordRepository> logger)
    {
        _context = context;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task<WorkRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _context.WorkRecords
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }, cancellationToken);
    }

    public async Task<IEnumerable<WorkRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _context.WorkRecords
                .OrderByDescending(x => x.ProcessingTime)
                .ToListAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<WorkRecord> AddAsync(WorkRecord workRecord, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            _context.WorkRecords.Add(workRecord);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registro {Id} salvo", workRecord.Id);
            return workRecord;
        }, cancellationToken);
    }

    public async Task UpdateAsync(WorkRecord workRecord, CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            _context.WorkRecords.Update(workRecord);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Registro {Id} atualizado", workRecord.Id);
        }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _context.WorkRecords
                .AnyAsync(x => x.Id == id, cancellationToken);
        }, cancellationToken);
    }
}