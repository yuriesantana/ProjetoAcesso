using ControleAcesso.Models;
using ControleAcesso.Persistencia;

namespace ControleAcesso;

/// <summary>
/// Mantém o cadastro de usuários e ambientes e controla a persistência dos dados.
/// </summary>
public class Cadastro
{
    private readonly List<Usuario> usuarios = new();
    private readonly List<Ambiente> ambientes = new();
    private readonly string caminhoBanco;

    public Cadastro(string caminhoBanco = "controle_acesso.xml")
    {
        this.caminhoBanco = caminhoBanco;
    }

    public IReadOnlyList<Usuario> Usuarios => usuarios.AsReadOnly();

    public IReadOnlyList<Ambiente> Ambientes => ambientes.AsReadOnly();

    // ---------------------------------------------------------------- Usuários

    public void AdicionarUsuario(Usuario usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (PesquisarUsuario(usuario) is not null)
        {
            throw new InvalidOperationException($"Já existe um usuário com o id {usuario.Id}.");
        }

        usuarios.Add(usuario);
    }

    /// <summary>
    /// Remove o usuário. Só é possível se ele existir e não possuir nenhuma
    /// permissão de acesso. Retorna true se a remoção foi realizada.
    /// </summary>
    public bool RemoverUsuario(Usuario usuario)
    {
        Usuario? existente = PesquisarUsuario(usuario);

        if (existente is null || existente.Ambientes.Count > 0)
        {
            return false;
        }

        return usuarios.Remove(existente);
    }

    /// <summary>Procura o usuário pelo id. Retorna null se não for encontrado.</summary>
    public Usuario? PesquisarUsuario(Usuario usuario)
    {
        if (usuario is null)
        {
            return null;
        }

        return usuarios.FirstOrDefault(u => u.Id == usuario.Id);
    }

    // --------------------------------------------------------------- Ambientes

    public void AdicionarAmbiente(Ambiente ambiente)
    {
        ArgumentNullException.ThrowIfNull(ambiente);

        if (PesquisarAmbiente(ambiente) is not null)
        {
            throw new InvalidOperationException($"Já existe um ambiente com o id {ambiente.Id}.");
        }

        ambientes.Add(ambiente);
    }

    /// <summary>
    /// Remove o ambiente (e, com ele, o seu log). As permissões de acesso que os
    /// usuários possuíam para o ambiente são revogadas. Retorna true se a remoção
    /// foi realizada.
    /// </summary>
    public bool RemoverAmbiente(Ambiente ambiente)
    {
        Ambiente? existente = PesquisarAmbiente(ambiente);

        if (existente is null)
        {
            return false;
        }

        foreach (Usuario usuario in usuarios)
        {
            usuario.RevogarPermissao(existente);
        }

        return ambientes.Remove(existente);
    }

    /// <summary>Procura o ambiente pelo id. Retorna null se não for encontrado.</summary>
    public Ambiente? PesquisarAmbiente(Ambiente ambiente)
    {
        if (ambiente is null)
        {
            return null;
        }

        return ambientes.FirstOrDefault(a => a.Id == ambiente.Id);
    }

    // ------------------------------------------------------------- Persistência

    /// <summary>Grava todos os dados do cadastro no banco (arquivo).</summary>
    public void Upload()
    {
        BancoDados.Salvar(caminhoBanco, usuarios, ambientes);
    }

    /// <summary>Carrega os dados gravados no banco (arquivo) para a memória.</summary>
    public void Download()
    {
        var dados = BancoDados.Carregar(caminhoBanco);

        usuarios.Clear();
        usuarios.AddRange(dados.Usuarios);

        ambientes.Clear();
        ambientes.AddRange(dados.Ambientes);
    }
}
