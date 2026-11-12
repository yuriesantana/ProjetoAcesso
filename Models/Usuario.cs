namespace ControleAcesso.Models;

/// <summary>
/// Usuário da empresa. Cada usuário possui, no máximo, uma permissão por ambiente.
/// </summary>
public class Usuario
{
    private readonly int id;
    private readonly string nome;
    private readonly List<Ambiente> ambientes = new();

    public Usuario(int id, string nome)
    {
        this.id = id;
        this.nome = nome ?? string.Empty;
    }

    public int Id => id;

    public string Nome => nome;

    /// <summary>Ambientes nos quais o usuário possui permissão de acesso.</summary>
    public IReadOnlyList<Ambiente> Ambientes => ambientes.AsReadOnly();

    /// <summary>
    /// Concede permissão de acesso ao ambiente.
    /// Retorna false se o ambiente for nulo ou se o usuário já possuir a permissão.
    /// </summary>
    public bool ConcederPermissao(Ambiente ambiente)
    {
        if (ambiente is null || PossuiPermissao(ambiente))
        {
            return false;
        }

        ambientes.Add(ambiente);
        return true;
    }

    /// <summary>
    /// Revoga a permissão de acesso ao ambiente.
    /// Retorna false se o ambiente for nulo ou se o usuário não possuir a permissão.
    /// </summary>
    public bool RevogarPermissao(Ambiente ambiente)
    {
        if (ambiente is null)
        {
            return false;
        }

        Ambiente? existente = ambientes.FirstOrDefault(a => a.Id == ambiente.Id);
        return existente is not null && ambientes.Remove(existente);
    }

    /// <summary>Indica se o usuário possui permissão de acesso ao ambiente.</summary>
    public bool PossuiPermissao(Ambiente ambiente)
    {
        return ambiente is not null && ambientes.Any(a => a.Id == ambiente.Id);
    }
}
