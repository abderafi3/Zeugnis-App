using FluentValidation;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Application.Validation;

public sealed class FormularValidator : AbstractValidator<Formular>
{
    public FormularValidator()
    {
        RuleFor(f => f.Anzeigename)
            .NotEmpty().WithMessage("Bitte einen Anzeigenamen für das Formular eingeben.")
            .MaximumLength(200);

        RuleFor(f => f.Jahrgang)
            .InclusiveBetween(0, 4).WithMessage("Der Jahrgang muss zwischen 0 (alle) und 4 liegen.");

        RuleFor(f => f.Ueberschrift).MaximumLength(500);

        RuleForEach(f => f.Freitextfelder).ChildRules(feld =>
        {
            feld.RuleFor(x => x.Name).NotEmpty().WithMessage("Jedes Freitextfeld braucht einen Namen.").MaximumLength(100);
            feld.RuleFor(x => x.MaxZeichen).GreaterThan(0).When(x => x.MaxZeichen is not null)
                .WithMessage("Das Zeichenlimit muss größer als 0 sein.");
        });

        RuleFor(f => f.Freitextfelder)
            .Must(list => list.Select(x => x.Name.Trim().ToLowerInvariant()).Distinct().Count() == list.Count)
            .WithMessage("Die Namen der Freitextfelder müssen eindeutig sein.");
    }
}
