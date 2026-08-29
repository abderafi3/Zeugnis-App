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

        RuleFor(f => f.Bereich)
            .NotEmpty().WithMessage("Bitte einen Bereich angeben.")
            .MaximumLength(100);

        RuleFor(f => f.Stufe)
            .InclusiveBetween(1, 4).WithMessage("Die Stufe muss zwischen 1 und 4 liegen.");
    }
}
