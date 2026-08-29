namespace ZeugnisApp.Domain.Entities;

/// <summary>
/// Number of lessons per weekday for a class (plan §4). Owned value object of <see cref="Klasse"/>.
/// Used to convert absence <em>days</em> into absence <em>hours</em> where a template needs hours.
/// </summary>
public class Wochenstundenplan
{
    public int Montag { get; set; }
    public int Dienstag { get; set; }
    public int Mittwoch { get; set; }
    public int Donnerstag { get; set; }
    public int Freitag { get; set; }

    public int WochenstundenGesamt => Montag + Dienstag + Mittwoch + Donnerstag + Freitag;
}
