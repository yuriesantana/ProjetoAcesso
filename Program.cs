using System.Text;
using ControleAcesso.Models;

namespace ControleAcesso;

public static class Program
{
    private static readonly Cadastro cadastro = new();

    /// <summary>Lançada quando a entrada padrão é encerrada (ex.: Ctrl+Z / Ctrl+D).</summary>
    private sealed class FimDeEntradaException : Exception
    {
    }

    public static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        // Carga dos dados ao executar a aplicação (download)
        try
        {
            cadastro.Download();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Não foi possível carregar os dados: {ex.Message}");
            Console.WriteLine("Aplicação encerrada sem alterar o arquivo de dados.");
            return 1;
        }

        // Garante a gravação dos dados também se a aplicação for interrompida com Ctrl+C
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Console.WriteLine();
            Encerrar();
            Environment.Exit(0);
        };

        try
        {
            ExecutarMenu();
        }
        catch (FimDeEntradaException)
        {
            Console.WriteLine();
        }

        Encerrar();
        return 0;
    }

    /// <summary>Persistência dos dados quando a aplicação é encerrada (upload).</summary>
    private static void Encerrar()
    {
        try
        {
            cadastro.Upload();
            Console.WriteLine("Dados salvos. Até logo!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar os dados: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------- Menu

    private static void ExecutarMenu()
    {
        int opcao;

        do
        {
            ExibirMenu();
            opcao = LerInt("Opção: ");
            Console.WriteLine();

            switch (opcao)
            {
                case 0:
                    break;
                case 1:
                    CadastrarAmbiente();
                    break;
                case 2:
                    ConsultarAmbiente();
                    break;
                case 3:
                    ExcluirAmbiente();
                    break;
                case 4:
                    CadastrarUsuario();
                    break;
                case 5:
                    ConsultarUsuario();
                    break;
                case 6:
                    ExcluirUsuario();
                    break;
                case 7:
                    ConcederPermissao();
                    break;
                case 8:
                    RevogarPermissao();
                    break;
                case 9:
                    RegistrarAcesso();
                    break;
                case 10:
                    ConsultarLogs();
                    break;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
        while (opcao != 0);
    }

    private static void ExibirMenu()
    {
        Console.WriteLine();
        Console.WriteLine("========== CONTROLE DE ACESSOS ==========");
        Console.WriteLine(" 1 - Cadastrar ambiente");
        Console.WriteLine(" 2 - Consultar ambiente");
        Console.WriteLine(" 3 - Excluir ambiente");
        Console.WriteLine(" 4 - Cadastrar usuário");
        Console.WriteLine(" 5 - Consultar usuário");
        Console.WriteLine(" 6 - Excluir usuário");
        Console.WriteLine(" 7 - Conceder permissão de acesso ao usuário");
        Console.WriteLine(" 8 - Revogar permissão de acesso ao usuário");
        Console.WriteLine(" 9 - Registrar acesso");
        Console.WriteLine("10 - Consultar logs de acesso");
        Console.WriteLine(" 0 - Sair");
        Console.WriteLine("=========================================");
    }

    // --------------------------------------------------------------- Ambientes

    private static void CadastrarAmbiente()
    {
        Console.WriteLine("--- Cadastrar ambiente ---");
        int id = LerId("Id do ambiente: ");

        if (BuscarAmbiente(id) is not null)
        {
            Console.WriteLine($"Já existe um ambiente com o id {id}.");
            return;
        }

        string nome = LerTexto("Nome do ambiente: ");
        cadastro.AdicionarAmbiente(new Ambiente(id, nome));
        Console.WriteLine($"Ambiente '{nome}' cadastrado com sucesso.");
    }

    private static void ConsultarAmbiente()
    {
        Console.WriteLine("--- Consultar ambiente ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        List<Usuario> comPermissao = cadastro.Usuarios.Where(u => u.PossuiPermissao(ambiente)).ToList();

        Console.WriteLine($"Id: {ambiente.Id}");
        Console.WriteLine($"Nome: {ambiente.Nome}");
        Console.WriteLine($"Logs registrados: {ambiente.Logs.Count} (máximo {Ambiente.MaxLogs})");
        Console.WriteLine("Usuários com permissão de acesso:");

        if (comPermissao.Count == 0)
        {
            Console.WriteLine("  (nenhum)");
        }

        foreach (Usuario usuario in comPermissao)
        {
            Console.WriteLine($"  [{usuario.Id}] {usuario.Nome}");
        }
    }

    private static void ExcluirAmbiente()
    {
        Console.WriteLine("--- Excluir ambiente ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        int permissoes = cadastro.Usuarios.Count(u => u.PossuiPermissao(ambiente));

        if (cadastro.RemoverAmbiente(ambiente))
        {
            Console.WriteLine($"Ambiente '{ambiente.Nome}' removido com sucesso.");

            if (permissoes > 0)
            {
                Console.WriteLine($"{permissoes} permissão(ões) de acesso a este ambiente foram revogadas.");
            }
        }
        else
        {
            Console.WriteLine("Não foi possível remover o ambiente.");
        }
    }

    // ---------------------------------------------------------------- Usuários

    private static void CadastrarUsuario()
    {
        Console.WriteLine("--- Cadastrar usuário ---");
        int id = LerId("Id do usuário: ");

        if (BuscarUsuario(id) is not null)
        {
            Console.WriteLine($"Já existe um usuário com o id {id}.");
            return;
        }

        string nome = LerTexto("Nome do usuário: ");
        cadastro.AdicionarUsuario(new Usuario(id, nome));
        Console.WriteLine($"Usuário '{nome}' cadastrado com sucesso.");
    }

    private static void ConsultarUsuario()
    {
        Console.WriteLine("--- Consultar usuário ---");
        Usuario? usuario = LerUsuarioExistente();
        if (usuario is null)
        {
            return;
        }

        Console.WriteLine($"Id: {usuario.Id}");
        Console.WriteLine($"Nome: {usuario.Nome}");
        Console.WriteLine("Ambientes com permissão de acesso:");

        if (usuario.Ambientes.Count == 0)
        {
            Console.WriteLine("  (nenhum)");
        }

        foreach (Ambiente ambiente in usuario.Ambientes)
        {
            Console.WriteLine($"  [{ambiente.Id}] {ambiente.Nome}");
        }
    }

    private static void ExcluirUsuario()
    {
        Console.WriteLine("--- Excluir usuário ---");
        Usuario? usuario = LerUsuarioExistente();
        if (usuario is null)
        {
            return;
        }

        if (cadastro.RemoverUsuario(usuario))
        {
            Console.WriteLine($"Usuário '{usuario.Nome}' removido com sucesso.");
        }
        else
        {
            Console.WriteLine(
                $"Não foi possível remover: o usuário possui permissão de acesso em {usuario.Ambientes.Count} ambiente(s). " +
                "Revogue as permissões antes de excluí-lo.");
        }
    }

    // -------------------------------------------------------------- Permissões

    private static void ConcederPermissao()
    {
        Console.WriteLine("--- Conceder permissão de acesso ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        Usuario? usuario = LerUsuarioExistente();
        if (usuario is null)
        {
            return;
        }

        if (usuario.ConcederPermissao(ambiente))
        {
            Console.WriteLine($"Permissão concedida: {usuario.Nome} -> {ambiente.Nome}.");
        }
        else
        {
            Console.WriteLine($"{usuario.Nome} já possui permissão de acesso a {ambiente.Nome}.");
        }
    }

    private static void RevogarPermissao()
    {
        Console.WriteLine("--- Revogar permissão de acesso ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        Usuario? usuario = LerUsuarioExistente();
        if (usuario is null)
        {
            return;
        }

        if (usuario.RevogarPermissao(ambiente))
        {
            Console.WriteLine($"Permissão revogada: {usuario.Nome} -> {ambiente.Nome}.");
        }
        else
        {
            Console.WriteLine($"{usuario.Nome} não possui permissão de acesso a {ambiente.Nome}.");
        }
    }

    // ------------------------------------------------------------------- Acessos

    private static void RegistrarAcesso()
    {
        Console.WriteLine("--- Registrar acesso ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        Usuario? usuario = LerUsuarioExistente();
        if (usuario is null)
        {
            return;
        }

        // O dispositivo do ambiente verifica as permissões do usuário e registra a ocorrência.
        bool autorizado = usuario.PossuiPermissao(ambiente);
        ambiente.RegistrarLog(new Log(DateTime.Now, usuario, autorizado));

        if (autorizado)
        {
            Console.WriteLine($"ACESSO AUTORIZADO: {usuario.Nome} acessou {ambiente.Nome}.");
        }
        else
        {
            Console.WriteLine($"ACESSO NEGADO: {usuario.Nome} não possui permissão para {ambiente.Nome}.");
        }

        Console.WriteLine("Ocorrência registrada no log do ambiente.");
    }

    private static void ConsultarLogs()
    {
        Console.WriteLine("--- Consultar logs de acesso ---");
        Ambiente? ambiente = LerAmbienteExistente();
        if (ambiente is null)
        {
            return;
        }

        Console.WriteLine("Filtro: 1 - Todos | 2 - Autorizados | 3 - Negados");
        int filtro;
        while (true)
        {
            filtro = LerInt("Filtro: ");
            if (filtro is >= 1 and <= 3)
            {
                break;
            }

            Console.WriteLine("Filtro inválido. Informe 1, 2 ou 3.");
        }

        IEnumerable<Log> logs = ambiente.Logs;
        logs = filtro switch
        {
            2 => logs.Where(l => l.TipoAcesso),
            3 => logs.Where(l => !l.TipoAcesso),
            _ => logs
        };

        List<Log> lista = logs.ToList();

        Console.WriteLine();
        Console.WriteLine($"Logs de '{ambiente.Nome}' - {lista.Count} registro(s), do mais antigo ao mais recente:");

        if (lista.Count == 0)
        {
            Console.WriteLine("  (nenhum registro)");
            return;
        }

        for (int i = 0; i < lista.Count; i++)
        {
            Log log = lista[i];
            string resultado = log.TipoAcesso ? "AUTORIZADO" : "NEGADO";
            Console.WriteLine(
                $"{i + 1,3}. {log.DtAcesso:dd/MM/yyyy HH:mm:ss} | {DescreverUsuario(log.Usuario),-32} | {resultado}");
        }
    }

    private static string DescreverUsuario(Usuario usuario)
    {
        return cadastro.Usuarios.Contains(usuario)
            ? $"{usuario.Nome} (id {usuario.Id})"
            : $"{usuario.Nome} (removido)";
    }

    // ----------------------------------------------------------------- Buscas

    private static Ambiente? BuscarAmbiente(int id)
    {
        return cadastro.PesquisarAmbiente(new Ambiente(id, string.Empty));
    }

    private static Usuario? BuscarUsuario(int id)
    {
        return cadastro.PesquisarUsuario(new Usuario(id, string.Empty));
    }

    private static Ambiente? LerAmbienteExistente()
    {
        int id = LerId("Id do ambiente: ");
        Ambiente? ambiente = BuscarAmbiente(id);

        if (ambiente is null)
        {
            Console.WriteLine($"Ambiente {id} não encontrado.");
        }

        return ambiente;
    }

    private static Usuario? LerUsuarioExistente()
    {
        int id = LerId("Id do usuário: ");
        Usuario? usuario = BuscarUsuario(id);

        if (usuario is null)
        {
            Console.WriteLine($"Usuário {id} não encontrado.");
        }

        return usuario;
    }

    // ---------------------------------------------------------------- Entrada

    private static string LerLinha(string mensagem)
    {
        Console.Write(mensagem);
        string? linha = Console.ReadLine();

        if (linha is null)
        {
            throw new FimDeEntradaException();
        }

        return linha.Trim();
    }

    private static int LerInt(string mensagem)
    {
        while (true)
        {
            if (int.TryParse(LerLinha(mensagem), out int valor))
            {
                return valor;
            }

            Console.WriteLine("Valor inválido. Informe um número inteiro.");
        }
    }

    private static int LerId(string mensagem)
    {
        while (true)
        {
            int id = LerInt(mensagem);

            if (id > 0)
            {
                return id;
            }

            Console.WriteLine("O id deve ser um número inteiro maior que zero.");
        }
    }

    private static string LerTexto(string mensagem)
    {
        while (true)
        {
            string texto = LerLinha(mensagem);

            if (texto.Length > 0)
            {
                return texto;
            }

            Console.WriteLine("O valor não pode ser vazio.");
        }
    }
}
