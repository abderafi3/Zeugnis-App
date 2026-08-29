using FluentValidation;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Validation;

public sealed class FloskelValidator : AbstractValidator<Floskel>
{
    public FloskelValidator()
    {
        RuleFor(f => f.Text)
            .NotEmpty().WithMessage("Bitte einen Text für die Floskel eingeben.")
            .MaximumLength(2000);

        // An empty Bereich means "gilt für alle Bereiche"; Stufe 0 means "gilt für alle Jahrgänge".
        RuleFor(f => f.Bereich)
            .MaximumLength(100);

        RuleFor(f => f.Stufe)
            .InclusiveBetween(0, 4).WithMessage("Die Stufe muss zwischen 0 (alle) und 4 liegen.");
    }
}
