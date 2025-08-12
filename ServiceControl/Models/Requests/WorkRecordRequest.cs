using System.ComponentModel.DataAnnotations;

namespace ServiceControl.Models.Requests;

public record WorkRecordRequest(
    [Required(ErrorMessage = "Serviço executado é obrigatório")]
    [StringLength(200, ErrorMessage = "Serviço executado deve ter no máximo 200 caracteres")]
    string ServicoExecutado,

    [Required(ErrorMessage = "Data é obrigatória")]
    DateTime Data,

    [Required(ErrorMessage = "Responsável é obrigatório")]
    [StringLength(100, ErrorMessage = "Responsável deve ter no máximo 100 caracteres")]
    string Responsavel,

    [Required(ErrorMessage = "Cidade é obrigatória")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Cidade deve ter entre 2 e 100 caracteres")]
    string Cidade);

public record BatchWorkRecordRequest(
    [Required]
    IEnumerable<WorkRecordRequest> Records,

    [Range(1, 1000, ErrorMessage = "Tamanho do lote deve ser entre 1 e 1000")]
    int BatchSize = 100);