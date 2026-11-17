using Microsoft.JSInterop;

namespace TripleA.Services;

/// <summary>Merkt sich Auswahl (Kanton, Gewässer) im Browser-Speicher des Handys.</summary>
public class Einstellungen(IJSRuntime js)
{
    public const string KantonSchluessel = "triplea.kanton";
    public const string GewaesserSchluessel = "triplea.gewaesser";

    public async Task<string?> LesenAsync(string schluessel)
    {
        try { return await js.InvokeAsync<string?>("localStorage.getItem", schluessel); }
        catch { return null; }
    }

    public async Task SchreibenAsync(string schluessel, string? wert)
    {
        try
        {
            if (string.IsNullOrEmpty(wert))
                await js.InvokeVoidAsync("localStorage.removeItem", schluessel);
            else
                await js.InvokeVoidAsync("localStorage.setItem", schluessel, wert);
        }
        catch { /* Speicher nicht verfügbar: Auswahl gilt nur für diese Sitzung */ }
    }
}
