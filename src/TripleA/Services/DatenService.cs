using System.Net.Http.Json;
using TripleA.Models;

namespace TripleA.Services;

/// <summary>
/// Lädt die JSON-Daten aus wwwroot/data und setzt die Regeln aus den drei Ebenen
/// Bund → Kanton → Gewässer zusammen.
/// </summary>
public class DatenService(HttpClient http)
{
    private Meta? meta;
    private List<Art>? arten;
    private Bund? bund;
    private List<KantonEintrag>? kantonListe;
    private readonly Dictionary<string, Kanton> kantone = new();
    private readonly Dictionary<string, Gewaesser> gewaesser = new();

    public async Task<Meta> GetMetaAsync() =>
        meta ??= await http.GetFromJsonAsync<Meta>("data/meta.json") ?? new Meta();

    public async Task<List<Art>> GetArtenAsync() =>
        arten ??= (await http.GetFromJsonAsync<List<Art>>("data/arten.json") ?? new())
            .OrderBy(a => SortierText(a.Name), StringComparer.Ordinal)
            .ToList();

    /// <summary>Sortierschlüssel, der Umlaute wie die Grundbuchstaben behandelt (Äsche bei A).</summary>
    public static string SortierText(string text) =>
        text.ToUpperInvariant().Replace("Ä", "A").Replace("Ö", "O").Replace("Ü", "U");

    public async Task<Art?> GetArtAsync(string id) =>
        (await GetArtenAsync()).FirstOrDefault(a => a.Id == id);

    public async Task<Bund> GetBundAsync() =>
        bund ??= await http.GetFromJsonAsync<Bund>("data/bund.json") ?? new Bund();

    public async Task<List<KantonEintrag>> GetKantonListeAsync() =>
        kantonListe ??= await http.GetFromJsonAsync<List<KantonEintrag>>("data/kantone/index.json") ?? new();

    public async Task<Kanton?> GetKantonAsync(string id)
    {
        if (kantone.TryGetValue(id, out var vorhanden)) return vorhanden;
        var kanton = await http.GetFromJsonAsync<Kanton>($"data/kantone/{id}.json");
        if (kanton is not null) kantone[id] = kanton;
        return kanton;
    }

    public async Task<Gewaesser?> GetGewaesserAsync(string id)
    {
        if (gewaesser.TryGetValue(id, out var vorhanden)) return vorhanden;
        var g = await http.GetFromJsonAsync<Gewaesser>($"data/gewaesser/{id}.json");
        if (g is not null) gewaesser[id] = g;
        return g;
    }

    public async Task<List<Gewaesser>> GetGewaesserVonKantonAsync(Kanton kanton)
    {
        var liste = new List<Gewaesser>();
        foreach (var id in kanton.GewaesserIds)
        {
            var g = await GetGewaesserAsync(id);
            if (g is not null) liste.Add(g);
        }
        return liste;
    }

    /// <summary>Regeln pro Art: Bund, überlagert vom Kanton, überlagert vom Gewässer (falls gewählt).</summary>
    public async Task<List<EffektiveArtRegel>> GetEffektiveRegelnAsync(Kanton kanton, Gewaesser? gew)
    {
        var alleArten = await GetArtenAsync();
        var bundDaten = await GetBundAsync();
        var ergebnis = new List<EffektiveArtRegel>();

        foreach (var art in alleArten)
        {
            ArtRegel regel = new() { ArtId = art.Id };
            string ebene = "";

            if (bundDaten.ArtRegeln.FirstOrDefault(r => r.ArtId == art.Id) is { } b)
            { regel = Ueberlagern(regel, b); ebene = "Bund"; }
            if (kanton.ArtRegeln.FirstOrDefault(r => r.ArtId == art.Id) is { } k)
            { regel = Ueberlagern(regel, k); ebene = "Kanton"; }
            if (gew?.ArtRegeln.FirstOrDefault(r => r.ArtId == art.Id) is { } g)
            { regel = Ueberlagern(regel, g); ebene = "Gewässer"; }

            ergebnis.Add(new EffektiveArtRegel { Art = art, Regel = regel, Ebene = ebene });
        }
        return ergebnis;
    }

    private static ArtRegel Ueberlagern(ArtRegel basis, ArtRegel neu) => new()
    {
        ArtId = basis.ArtId,
        SchonmassCm = neu.SchonmassCm ?? basis.SchonmassCm,
        SchonzeitVon = neu.SchonzeitVon ?? basis.SchonzeitVon,
        SchonzeitBis = neu.SchonzeitBis ?? basis.SchonzeitBis,
        GanzjaehrigGeschont = neu.GanzjaehrigGeschont ?? basis.GanzjaehrigGeschont,
        FangLimiteProTag = neu.FangLimiteProTag ?? basis.FangLimiteProTag,
        // Bemerkungen gelten nur für ihre eigene Ebene (sonst passen z.B. Bundes-Hinweise nicht zu kantonalen Daten)
        Bemerkung = neu.Bemerkung,
        Quelle = neu.Quelle ?? basis.Quelle
    };

    /// <summary>Wandelt "03-01" in "1.3." um.</summary>
    public static string Datum(string? mmtt)
    {
        if (string.IsNullOrWhiteSpace(mmtt)) return "";
        var teile = mmtt.Split('-');
        return teile.Length == 2 && int.TryParse(teile[0], out var m) && int.TryParse(teile[1], out var t)
            ? $"{t}.{m}."
            : mmtt;
    }
}
