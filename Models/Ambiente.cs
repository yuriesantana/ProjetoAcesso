namespace ControleAcesso.Models;

/// <summary>
/// Ambiente (sala) controlado por um dispositivo de acesso.
/// Mantém no máximo <see cref="MaxLogs"/> ocorrências, descartando as mais antigas.
/// </summary>
public class Ambiente
{
    /// <summary>Quantidade máxima de logs mantidos por ambiente.</summary>
    public const int MaxLogs = 100;

    private readonly int id;
    private readonly string nome;
    private readonly Queue<Log> logs = new();

    public Ambiente(int id, string nome)
    {
        this.id = id;
        this.nome = nome ?? string.Empty;
    }

    public int Id => id;

    public string Nome => nome;

    /// <summary>Logs do ambiente, do mais antigo para o mais recente.</summary>
    public IReadOnlyCollection<Log> Logs => logs;

    /// <summary>
    /// Registra uma ocorrência. Se o limite de <see cref="MaxLogs"/> já tiver sido
    /// atingido, o log mais antigo é descartado.
    /// </summary>
    public void RegistrarLog(Log log)
    {
        ArgumentNullException.ThrowIfNull(log);

        while (logs.Count >= MaxLogs)
        {
            logs.Dequeue();
        }

        logs.Enqueue(log);
    }
}
