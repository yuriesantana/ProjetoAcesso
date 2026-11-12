namespace ControleAcesso.Models;

/// <summary>
/// Ocorrência registrada por um dispositivo de ambiente
/// (acesso realizado ou tentativa de acesso negada).
/// </summary>
public class Log
{
    private readonly DateTime dtAcesso;
    private readonly Usuario usuario;
    private readonly bool tipoAcesso; // true = Autorizado, false = Negado

    public Log(DateTime dtAcesso, Usuario usuario, bool tipoAcesso)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        this.dtAcesso = dtAcesso;
        this.usuario = usuario;
        this.tipoAcesso = tipoAcesso;
    }

    public DateTime DtAcesso => dtAcesso;

    public Usuario Usuario => usuario;

    /// <summary>true = Autorizado, false = Negado.</summary>
    public bool TipoAcesso => tipoAcesso;
}
