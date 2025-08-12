using ServiceControl.Application.DTOs;

namespace ServiceControl.Application.UseCases.ProcessWorkRecord;

public interface IProcessWorkRecordUseCase
{
    Task<WorkRecordResponseDto> ExecuteAsync(WorkRecordRequestDto request, CancellationToken cancellationToken = default);
}

public interface IBatchProcessWorkRecordUseCase
{
    Task<BatchProcessResponseDto> ExecuteAsync(BatchProcessRequestDto request, CancellationToken cancellationToken = default);
}