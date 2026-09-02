using FluentValidation;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Validation;

public sealed class FoerderempfehlungValidator : AbstractValidator<Foerderempfehlung>
{
    public FoerderempfehlungValidator()
    {
        RuleFor(e => e.KindId).NotEmpty();

        RuleFor(e => e.Schuljahr)
            .NotEmpty().WithMessage("Bitte ein Schuljahr angeben.")
            .MaximumLength(20);

        RuleFor(e => e.Fach)
            .NotEmpty().WithMessage("Bitte ein Fach angeben.")
            .MaximumLength(100);

        RuleFor(e => e.Beobachtungen).MaximumLength(4000);
        RuleFor(e => e.SchulischeMassnahmen).MaximumLength(4000);
        RuleFor(e => e.EmpfehlungenFuerZuhause).MaximumLength(4000);
    }
}
