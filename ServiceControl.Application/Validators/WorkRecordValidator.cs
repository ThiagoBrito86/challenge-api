using ServiceControl.Application.DTOs;
using FluentValidation;

namespace ServiceControl.Application.Validators;

public class WorkRecordRequestValidator : AbstractValidator<WorkRecordRequestDto>
{
    public WorkRecordRequestValidator()
    {
        RuleFor(x => x.ServicoExecutado)
            .NotEmpty().WithMessage("Serviço executado é obrigatório")
            .MaximumLength(200).WithMessage("Serviço executado deve ter no máximo 200 caracteres");

        RuleFor(x => x.Data)
            .NotEmpty().WithMessage("Data é obrigatória")
            .LessThanOrEqualTo(DateTime.Now.AddDays(1)).WithMessage("Data não pode ser no futuro");

        RuleFor(x => x.Responsavel)
            .NotEmpty().WithMessage("Responsável é obrigatório")
            .MaximumLength(100).WithMessage("Responsável deve ter no máximo 100 caracteres");

        RuleFor(x => x.Cidade)
            .NotEmpty().WithMessage("Cidade é obrigatória")
            .MinimumLength(2).WithMessage("Cidade deve ter pelo menos 2 caracteres")
            .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres");
    }
}