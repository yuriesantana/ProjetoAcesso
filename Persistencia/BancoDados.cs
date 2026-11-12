using System.Data;
using ControleAcesso.Models;

namespace ControleAcesso.Persistencia;

/// <summary>
/// Persistência em um modelo relacional (System.Data), gravado em um arquivo XML.
///
/// Modelo:
///   ambiente  (id PK, nome)
///   usuario   (id PK, nome)
///   permissao (usuario_id FK, ambiente_id FK, PK composta)  -> 1 permissão por usuário/ambiente
///   log       (id PK, ambiente_id FK, usuario_id FK null, usuario_nome, dt_acesso, autorizado)
///
/// Integridade:
///   - excluir um ambiente remove suas permissões e seus logs (CASCADE);
///   - excluir um usuário remove suas permissões (CASCADE) e preserva seus logs,
///     que ficam com usuario_id nulo e o nome gravado em usuario_nome (SET NULL).
/// </summary>
public static class BancoDados
{
    private const string TAmbiente = "ambiente";
    private const string TUsuario = "usuario";
    private const string TPermissao = "permissao";
    private const string TLog = "log";

    /// <summary>Grava (substitui) todo o conteúdo do banco.</summary>
    public static void Salvar(string caminho, IEnumerable<Usuario> usuarios, IEnumerable<Ambiente> ambientes)
    {
        List<Usuario> listaUsuarios = usuarios.ToList();
        List<Ambiente> listaAmbientes = ambientes.ToList();
        var usuariosCadastrados = new HashSet<Usuario>(listaUsuarios); // comparação por referência

        DataSet ds = CriarEsquema();
        DataTable tAmbiente = ds.Tables[TAmbiente]!;
        DataTable tUsuario = ds.Tables[TUsuario]!;
        DataTable tPermissao = ds.Tables[TPermissao]!;
        DataTable tLog = ds.Tables[TLog]!;

        foreach (Ambiente ambiente in listaAmbientes)
        {
            tAmbiente.Rows.Add(ambiente.Id, ambiente.Nome);
        }

        foreach (Usuario usuario in listaUsuarios)
        {
            tUsuario.Rows.Add(usuario.Id, usuario.Nome);
        }

        foreach (Usuario usuario in listaUsuarios)
        {
            foreach (Ambiente ambiente in usuario.Ambientes)
            {
                tPermissao.Rows.Add(usuario.Id, ambiente.Id);
            }
        }

        int idLog = 1;
        foreach (Ambiente ambiente in listaAmbientes)
        {
            foreach (Log log in ambiente.Logs)
            {
                object usuarioId = usuariosCadastrados.Contains(log.Usuario)
                    ? log.Usuario.Id
                    : DBNull.Value;

                tLog.Rows.Add(idLog++, ambiente.Id, usuarioId, log.Usuario.Nome, log.DtAcesso, log.TipoAcesso);
            }
        }

        string? pasta = Path.GetDirectoryName(Path.GetFullPath(caminho));
        if (!string.IsNullOrEmpty(pasta))
        {
            Directory.CreateDirectory(pasta);
        }

        // Grava em arquivo temporário e troca no final, para não corromper
        // o banco existente caso algo falhe durante a escrita.
        string temporario = caminho + ".tmp";
        ds.WriteXml(temporario, XmlWriteMode.WriteSchema);
        File.Move(temporario, caminho, overwrite: true);
    }

    /// <summary>Lê o banco e reconstrói os objetos de domínio. Se o arquivo não existir, retorna listas vazias.</summary>
    public static (List<Usuario> Usuarios, List<Ambiente> Ambientes) Carregar(string caminho)
    {
        var usuarios = new List<Usuario>();
        var ambientes = new List<Ambiente>();

        if (!File.Exists(caminho))
        {
            return (usuarios, ambientes);
        }

        DataSet ds = CriarEsquema();
        ds.ReadXml(caminho, XmlReadMode.IgnoreSchema);

        var ambientesPorId = new Dictionary<int, Ambiente>();
        foreach (DataRow linha in ds.Tables[TAmbiente]!.Select("", "id ASC"))
        {
            var ambiente = new Ambiente(linha.Field<int>("id"), linha.Field<string>("nome")!);
            ambientes.Add(ambiente);
            ambientesPorId[ambiente.Id] = ambiente;
        }

        var usuariosPorId = new Dictionary<int, Usuario>();
        foreach (DataRow linha in ds.Tables[TUsuario]!.Select("", "id ASC"))
        {
            var usuario = new Usuario(linha.Field<int>("id"), linha.Field<string>("nome")!);
            usuarios.Add(usuario);
            usuariosPorId[usuario.Id] = usuario;
        }

        foreach (DataRow linha in ds.Tables[TPermissao]!.Rows)
        {
            Usuario usuario = usuariosPorId[linha.Field<int>("usuario_id")];
            Ambiente ambiente = ambientesPorId[linha.Field<int>("ambiente_id")];
            usuario.ConcederPermissao(ambiente);
        }

        // Os logs são recarregados na ordem em que foram gravados (do mais antigo ao mais recente).
        foreach (DataRow linha in ds.Tables[TLog]!.Select("", "id ASC"))
        {
            Ambiente ambiente = ambientesPorId[linha.Field<int>("ambiente_id")];
            int? usuarioId = linha.Field<int?>("usuario_id");

            // Usuário já excluído: o log é mantido com o nome que foi gravado.
            Usuario usuario = usuarioId.HasValue
                ? usuariosPorId[usuarioId.Value]
                : new Usuario(0, linha.Field<string>("usuario_nome")!);

            ambiente.RegistrarLog(new Log(
                linha.Field<DateTime>("dt_acesso"),
                usuario,
                linha.Field<bool>("autorizado")));
        }

        return (usuarios, ambientes);
    }

    /// <summary>Define as tabelas, chaves primárias e chaves estrangeiras do modelo relacional.</summary>
    private static DataSet CriarEsquema()
    {
        var ds = new DataSet("ControleAcesso");

        // ambiente
        DataTable ambiente = ds.Tables.Add(TAmbiente);
        DataColumn ambienteId = ambiente.Columns.Add("id", typeof(int));
        ambiente.Columns.Add("nome", typeof(string)).AllowDBNull = false;
        ambiente.PrimaryKey = new[] { ambienteId };

        // usuario
        DataTable usuario = ds.Tables.Add(TUsuario);
        DataColumn usuarioId = usuario.Columns.Add("id", typeof(int));
        usuario.Columns.Add("nome", typeof(string)).AllowDBNull = false;
        usuario.PrimaryKey = new[] { usuarioId };

        // permissao: a PK composta garante uma única permissão por usuário/ambiente
        DataTable permissao = ds.Tables.Add(TPermissao);
        DataColumn permUsuarioId = permissao.Columns.Add("usuario_id", typeof(int));
        DataColumn permAmbienteId = permissao.Columns.Add("ambiente_id", typeof(int));
        permissao.PrimaryKey = new[] { permUsuarioId, permAmbienteId };
        permissao.Constraints.Add(CriarChaveEstrangeira("fk_permissao_usuario", usuarioId, permUsuarioId, Rule.Cascade));
        permissao.Constraints.Add(CriarChaveEstrangeira("fk_permissao_ambiente", ambienteId, permAmbienteId, Rule.Cascade));

        // log
        DataTable log = ds.Tables.Add(TLog);
        DataColumn logId = log.Columns.Add("id", typeof(int));
        DataColumn logAmbienteId = log.Columns.Add("ambiente_id", typeof(int));
        DataColumn logUsuarioId = log.Columns.Add("usuario_id", typeof(int));
        logUsuarioId.AllowDBNull = true;
        log.Columns.Add("usuario_nome", typeof(string)).AllowDBNull = false;
        DataColumn dtAcesso = log.Columns.Add("dt_acesso", typeof(DateTime));
        dtAcesso.DateTimeMode = DataSetDateTime.Unspecified; // preserva o horário local exatamente como registrado
        log.Columns.Add("autorizado", typeof(bool));
        log.PrimaryKey = new[] { logId };
        log.Constraints.Add(CriarChaveEstrangeira("fk_log_ambiente", ambienteId, logAmbienteId, Rule.Cascade));
        log.Constraints.Add(CriarChaveEstrangeira("fk_log_usuario", usuarioId, logUsuarioId, Rule.SetNull));

        return ds;
    }

    private static ForeignKeyConstraint CriarChaveEstrangeira(
        string nome, DataColumn pai, DataColumn filha, Rule regraExclusao)
    {
        return new ForeignKeyConstraint(nome, pai, filha)
        {
            DeleteRule = regraExclusao,
            UpdateRule = Rule.Cascade
        };
    }
}
