namespace ZeugnisApp.App.ViewModels.Klassen;

/// <summary>Drives the master/detail flow inside the Klassen area (list ↔ class form ↔ child list ↔ child form).</summary>
public interface IKlassenNavigator
{
    void ShowKlassenListe();
    void EditKlasse(Guid? klasseId);
    void ShowKinder(Guid klasseId, string klasseName);
    void EditKind(Guid klasseId, string klasseName, Guid? kindId);
    void ImportKinder(Guid klasseId, string klasseName);
}

/// <summary>Edit view models that hold unsaved buffered changes flush them before navigation leaves.</summary>
public interface IAsyncFlushable
{
    Task FlushAsync();
}
