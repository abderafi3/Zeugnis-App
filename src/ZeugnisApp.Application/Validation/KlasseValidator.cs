using FluentValidation;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Validation;

public sealed class KlasseValidator : AbstractValidator<Klasse>
{
    public KlasseValidator()
    {
        RuleFor(k => k.Name)
            .NotEmpty().WithMessage("Bitte einen Namen für die Klasse eingeben.")
            .MaximumLength(50);

        RuleFor(k => k.Jahrgang)
            .InclusiveBetween(1, 4).WithMessage("Der Jahrgang muss zwischen 1 und 4 liegen.");

        RuleFor(k => k.Klassenleitung).MaximumLength(200);

        const string stundenMsg = "Die Stundenzahl je Tag muss zwischen 0 und 20 liegen.";
        RuleFor(k => k.Wochenstundenplan.Montag).InclusiveBetween(0, 20).WithMessage(stundenMsg);
        RuleFor(k => k.Wochenstundenplan.Dienstag).InclusiveBetween(0, 20).WithMessage(stundenMsg);
        RuleFor(k => k.Wochenstundenplan.Mittwoch).InclusiveBetween(0, 20).WithMessage(stundenMsg);
        RuleFor(k => k.Wochenstundenplan.Donnerstag).InclusiveBetween(0, 20).WithMessage(stundenMsg);
        RuleFor(k => k.Wochenstundenplan.Freitag).InclusiveBetween(0, 20).WithMessage(stundenMsg);
    }
}
