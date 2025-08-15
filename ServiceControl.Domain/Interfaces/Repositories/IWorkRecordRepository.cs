using ServiceControl.Domain.Entities;

namespace ServiceControl.Domain.Intefaces.Repositories;

public interface IWorkRecordRepository
{
    Task<WorkRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkRecord>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkRecord> AddAsync(WorkRecord workRecord, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkRecord workRecord, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}