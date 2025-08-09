# EF Core Audit Logging (DE)

Bibliothek für EF Core, die Änderungen an Entities in klaren, anpassbaren Nachrichten protokolliert.

## Features

- Property-Änderungen (Modified):
    - Meldung: „{DisplayName}: {Old} -> {New}“
    - Anzeigename per [AuditDisplay]
- Sammlungsänderungen:
    - 1:n (z. B. User.Pets): „Schnuffi (Haustier) wurde zu Haustiere hinzugefügt/entfernt“
    - n:m (Skip Navigations): wird ebenfalls erkannt und als obige Nachricht protokolliert
- Entitätstitel für natürliche Sprache:
    - Per [AuditEntityTitle] (eine Property) oder [AuditEntityTitleTemplate("{Name} ({Id})")] (Template mit Platzhaltern)
- Entitätsnamen (Singular/Plural) via [AuditEntityDisplay]
- Ausnahmen/Ignore via [AuditIgnore]
- Owned Types: Property-Änderungen werden protokolliert
- Delete-Kurzmeldung optional, Join-Entities werden nicht als „gelöscht“ gemeldet

## Installation & Setup

1) DI-Registrierung und Optionen:

```csharp
services.AddEfCoreAuditLogging(opts =>
{
        // Optional: Templates anpassen
        // opts.PropertyChangeTemplate = "{DisplayName}: {Old} -> {New}";
});
```

2) DbContext-Interceptor aktivieren:

```csharp
services.AddDbContext<AppDbContext>((sp, o) => o
        .UseSqlite("Data Source=sample.db")
        .UseAuditLogging(sp));
```

## Attribute-Referenz

- [AuditDisplay("Anzeigename")]
    - Auf Properties oder Sammlungen (Skip/Collection Navigations) nutzbar
    - Steuert den Namen in Meldungen, z. B. „Haustiere“ statt „Pets“

- [AuditEntityDisplay("Singular", "Plural")]
    - Auf Entitätsklassen
    - Wird in Sammlungs-Meldungen als „({EntitySingular})" genutzt

- [AuditEntityTitle]
    - Markiert die Titel-Property, z. B. „Name“

- [AuditEntityTitleTemplate("{Name} ({Id})")]
    - Auf Entitätsklassen
    - Template-Platzhalter in {}:
        - Support für verschachtelte Pfade: {Owner.Name}
        - Fallback auf [AuditEntityTitle], dann Klassenname

- [AuditIgnore]
    - Unterdrückt das Logging dieser Property

## Optionen (AuditOptions)

- PropertyChangeTemplate (Default über Localizer: "{DisplayName}: {Old} -> {New}")
- CollectionAddedTemplate (Default über Localizer: "{Title} ({EntitySingular}) was added to {CollectionDisplay}")
- CollectionRemovedTemplate (Default über Localizer: "{Title} ({EntitySingular}) was removed from {CollectionDisplay}")
- VerboseDelete (Default: true)
- IncludeUnchangedMarkedProperties (Default: false)

Tokens in Templates:
- {DisplayName}, {Old}, {New}
- {Title}, {EntitySingular}, {CollectionDisplay}

## Mehrsprachigkeit und Formatierung der Werte

Die Bibliothek unterstützt Lokalisierung. Standardmäßig ist Englisch aktiviert. Sie können den Localizer global in der DI-Registrierung oder pro Options-Instanz festlegen.

- Standard-Localizer: Englisch
- Verfügbare Localizer: Englisch, Deutsch

Localizer in DI wählen (empfohlen):

```csharp
// Deutsch global aktivieren
services.AddEfCoreAuditLogging<GermanAuditLocalizer>();
```

Oder pro Options-Instanz setzen:

```csharp
services.AddEfCoreAuditLogging(opts =>
{
    opts.Localizer = new GermanAuditLocalizer();
});
```

Hinweise:
- Wenn ein Template in AuditOptions leer ist, wird das Template des aktuellen Localizers verwendet.
- Wertformatierung (Null-Symbol und Bool-Werte) wird vom Localizer vorgegeben.

Wertformatierung (Englischer Localizer):
- null → „∅“
- DateTime → „yyyy-MM-dd HH:mm:ss“
- DateOnly → „yyyy-MM-dd“
- TimeOnly → „HH:mm:ss“
- bool → „Yes“/„No“
- Enum → ToString()

## Unterstützte Änderungen

- Modified-Properties mit Delta
- 1:n Collection Add/Remove (über FK + inverse Collection)
- n:m Add/Remove (über Skip Navigations/Join-Entities)
- Owned Types Property-Änderungen
- Delete-Kurzmeldung (optional)

## Beispiele

```csharp
[AuditEntityDisplay("Haustier", "Haustiere")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
        public int Id { get; set; }
        [AuditEntityTitle]
        [AuditDisplay("Name")]
        public string Name { get; set; } = string.Empty;
}

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class User
{
        public int Id { get; set; }
        [AuditDisplay("Haustiere")]
        public List<Pet> Pets { get; set; } = new();
}
```

Ergebnisbeispiele:
- Property: „Name: Bello -> Schnuffi“
- 1:n Add: „Schnuffi (Haustier) wurde zu Haustiere hinzugefügt“
- 1:n Remove: „Schnuffi (Haustier) wurde von Haustiere entfernt“
- n:m Add/Remove analog mit Skip-Navigations

## Erweiterbarkeit

- IAuditEventSink: Ziel der Nachrichten (Default: Logger)
- Eigene Sinks: z. B. Datenbank/Datei

## Tests (Abdeckung)

- Property-Änderungen inkl. [AuditDisplay], [AuditIgnore], Custom-Template
- 1:n Collection Add/Remove
- n:m (Skip Navigations) Add/Remove, inkl. Composite-Key-Beispiel
- Owned Types Property-Änderungen
- Delete-Kurzmeldung
- Datentypen: DateTime, DateOnly, TimeOnly, bool, decimal, Enum

Alle Tests laufen grün (xUnit, InMemory Provider).

## Roadmap

- Mehrsprachigkeit/Localization (Ressourcen/IStringLocalizer)
- Persistente Audit Trails (DB-Sink + Migrationsbeispiel)

## Beispiele

### 1) Minimales Modell und englische Standardeinstellungen

```csharp
using EF.Core.HumanReadableLog.Attributes;

[AuditEntityDisplay("User", "Users")]
public class User
{
    public int Id { get; set; }
    [AuditDisplay("Name"), AuditEntityTitle]
    public string DisplayName { get; set; } = string.Empty;
    [AuditDisplay("Pets")]
    public List<Pet> Pets { get; set; } = new();
}

[AuditEntityDisplay("Pet", "Pets")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
    public int Id { get; set; }
    [AuditDisplay("Name")]
    public string Name { get; set; } = string.Empty;
}
```

Registrierung mit englischem Standard und Verwendung des Interceptors:

```csharp
services.AddEfCoreAuditLogging(); // Englisch ist der Standard
services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlite("Data Source=sample.db").UseAuditLogging(sp));
```

Beispielaktionen und resultierende Nachrichten (Englisch):

- Property-Änderung: Name von Max zu Moritz
    - Output: `Name: Max -> Moritz`
- Hinzufügen zur Sammlung: Schnuffi zu Pets
    - Output: `Schnuffi (Pet) was added to Pets`
- Entfernen aus der Sammlung: Schnuffi aus Pets
    - Output: `Schnuffi (Pet) was removed from Pets`

Bool- und Nullformatierung (Englisch):
- `Yes` / `No` für bool, `∅` für null

### 2) Umstellung auf Deutsch

Deutsch per DI wählen:

```csharp
using EF.Core.HumanReadableLog.Localization;

services.AddEfCoreAuditLogging<GermanAuditLocalizer>();
```

Mit deutschen Anzeigenamen:

```csharp
[AuditEntityDisplay("Haustier", "Haustiere")]
public class Pet { /* ... */ }

public class User
{
    // ...
    [AuditDisplay("Haustiere")] // Sammlungsbezeichnung
    public List<Pet> Pets { get; set; } = new();
}
```

Resultierende Nachrichten (Deutsch):

- `Name: Max -> Moritz`
- `Schnuffi (Haustier) wurde zu Haustiere hinzugefügt`
- `Schnuffi (Haustier) wurde von Haustiere entfernt`
- Bool: `Ja` / `Nein`

### 3) Override per Options und eigenes Template

```csharp
services.AddEfCoreAuditLogging(opts =>
{
    opts.Localizer = new GermanAuditLocalizer();
    // Eigenes Property-Template (unabhängig vom Localizer)
    opts.PropertyChangeTemplate = "Changed: {DisplayName} from {Old} to {New}";
});
```

Property-Änderung Ausgabe:
- `Changed: Name from Max to Moritz`

Verfügbare Tokens:
- Property: `{DisplayName}`, `{Old}`, `{New}`
- Collection: `{Title}`, `{EntitySingular}`, `{CollectionDisplay}`
