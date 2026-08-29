using ZeugnisApp.Application.Formulare;
using ZeugnisApp.Domain.Entities;

namespace ZeugnisApp.Infrastructure.Persistence;

/// <summary>
/// Seeds the seven standard templates (plan §Sprint 5) once into an empty database so a teacher
/// starts with editable metadata; the real <c>.docx</c> files are linked in Sprint 6. Deleting them
/// does not re-seed. The definitions live in <see cref="StandardFormulare"/>.
/// </summary>
internal static class FormularSeed
{
    public static IReadOnlyList<Formular> Standardformulare() => StandardFormulare.All();
}
