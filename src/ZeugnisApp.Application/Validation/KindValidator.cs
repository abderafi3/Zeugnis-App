using FluentValidation;
using ZeugnisApp.Domain.Entities;
using ZeugnisApp.Domain.Enums;

namespace ZeugnisApp.Application.Validation;

public sealed class KindValidator : AbstractValidator<Kind>
{
    public KindValidator()
    {
        RuleFor(k => k.Vorname)
            .NotEmpty().WithMessage("Bitte einen Vornamen eingeben.")
            .MaximumLength(100);

        RuleFor(k => k.Nachname)
            .NotEmpty().WithMessage("Bitte einen Nachnamen eingeben.")
            .MaximumLength(100);

        RuleFor(k => k.KlasseId).NotEmpty();

        RuleFor(k => k.Geburtsdatum)
            .LessThan(_ => DateOnly.FromDateTime(DateTime.Today))
            .When(k => k.Geburtsdatum is not null)
            .WithMessage("Das Geburtsdatum muss in der Vergangenheit liegen.");

        RuleFor(k => k.Foerderschwerpunkt)
            .MaximumLength(500)
            .Must(string.IsNullOrWhiteSpace)
            .When(k => k.Foerderstatus == Foerderstatus.Regel)
            .WithMessage("Ein Förderschwerpunkt ist nur bei sonderpädagogischer Unterstützung möglich.");
    }
}
