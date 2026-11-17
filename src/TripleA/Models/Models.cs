namespace TripleA.Models;

/// <summary>Herkunft einer Angabe: Titel, Link und Stand (Datum).</summary>
public class Quelle
{
    public string Titel { get; set; } = "";
    public string? Url { get; set; }
    public string? Stand { get; set; }
}

/// <summary>Version und Stand des gesamten Datensatzes.</summary>
public class Meta
{
    public string Version { get; set; } = "";
    public string Stand { get; set; } = "";
}

/// <summary>Eine Fischart im Lexikon (Reiter "Tiere").</summary>
public class Art
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Familie { get; set; }
    public string? Herkunft { get; set; }
    public string? Bild { get; set; }
    public string? BildQuelle { get; set; }
    public string? Merkmale { get; set; }
    public string? Groesse { get; set; }
    public string? Lebensraum { get; set; }
    public string? Nahrung { get; set; }
    public string? Laichzeit { get; set; }
    public string? Koeder { get; set; }
    public string? Gefaehrdung { get; set; }
    public string? Verwechslung { get; set; }
}

/// <summary>Eine Regel in Textform (Köder, Hilfsmittel, Hinweise).</summary>
public class Regel
{
    public string Text { get; set; } = "";
    public Quelle? Quelle { get; set; }
}

/// <summary>Regel zu einer Art. Leere Felder werden von der übergeordneten Ebene übernommen.</summary>
public class ArtRegel
{
    public string ArtId { get; set; } = "";
    public int? SchonmassCm { get; set; }
    /// <summary>Beginn der Schonzeit im Format MM-TT.</summary>
    public string? SchonzeitVon { get; set; }
    /// <summary>Ende der Schonzeit im Format MM-TT.</summary>
    public string? SchonzeitBis { get; set; }
    public bool? GanzjaehrigGeschont { get; set; }
    public int? FangLimiteProTag { get; set; }
    public string? Bemerkung { get; set; }
    public Quelle? Quelle { get; set; }
}

/// <summary>Vorgaben des Bundes (unterste Ebene).</summary>
public class Bund
{
    public bool Erfasst { get; set; }
    public List<Regel> AllgemeineRegeln { get; set; } = new();
    public List<ArtRegel> ArtRegeln { get; set; } = new();
    public List<Quelle> Quellen { get; set; } = new();
}

/// <summary>Eintrag in der Kantonsliste.</summary>
public class KantonEintrag
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kuerzel { get; set; } = "";
}

/// <summary>Kantonale Regeln (mittlere Ebene).</summary>
public class Kanton
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kuerzel { get; set; } = "";
    public string? System { get; set; }
    public string? Zugang { get; set; }
    public bool Erfasst { get; set; }
    public List<Regel> Hinweise { get; set; } = new();
    public List<Regel> KoederRegeln { get; set; } = new();
    public List<ArtRegel> ArtRegeln { get; set; } = new();
    public List<string> GewaesserIds { get; set; } = new();
    public List<Quelle> Quellen { get; set; } = new();
}

/// <summary>Gewässer mit eigenen Regeln (oberste Ebene). Kann mehreren Kantonen gehören.</summary>
public class Gewaesser
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Typ { get; set; }
    public List<string> Kantone { get; set; } = new();
    public string? Zugang { get; set; }
    public bool Erfasst { get; set; }
    /// <summary>Pachtrevier: Die App zeigt nur einen Verweis auf den Pachtverein.</summary>
    public bool NurVerweis { get; set; }
    public Quelle? Verweis { get; set; }
    public List<Regel> Hinweise { get; set; } = new();
    public List<Regel> KoederRegeln { get; set; } = new();
    public List<ArtRegel> ArtRegeln { get; set; } = new();
    public List<Quelle> Quellen { get; set; } = new();
}

/// <summary>Zusammengesetzte Regel für eine Art, mit der Ebene, aus der sie zuletzt stammt.</summary>
public class EffektiveArtRegel
{
    public required Art Art { get; init; }
    public required ArtRegel Regel { get; init; }
    public required string Ebene { get; init; }

    public bool HatAngaben =>
        Regel.SchonmassCm is not null || Regel.SchonzeitVon is not null ||
        Regel.GanzjaehrigGeschont == true || Regel.FangLimiteProTag is not null ||
        !string.IsNullOrWhiteSpace(Regel.Bemerkung);
}
