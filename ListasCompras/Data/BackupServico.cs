using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Data;

// O banco é um arquivo só. O que torna backup e restauração delicados é o WAL:
// as gravações recentes podem estar no -wal e não no .db principal.
public static class BackupServico
{
    public static string CaminhoDoBanco(AppDbContext context)
    {
        var conexao = new SqliteConnectionStringBuilder(context.Database.GetConnectionString());
        return conexao.DataSource;
    }

    /// <summary>
    /// Copia o banco para um arquivo temporário usando o VACUUM INTO do SQLite, que
    /// gera um arquivo íntegro e já consolidado — copiar o .db na mão poderia deixar
    /// de fora o que ainda está no journal WAL.
    /// </summary>
    public static string GerarCopia(AppDbContext context)
    {
        var destino = Path.Combine(Path.GetTempPath(), $"pingo-os-backup-{Guid.NewGuid():N}.db");

        using var conexao = new SqliteConnection($"Data Source={CaminhoDoBanco(context)}");
        conexao.Open();
        using var comando = conexao.CreateCommand();
        comando.CommandText = "VACUUM INTO $destino;";
        comando.Parameters.AddWithValue("$destino", destino);
        comando.ExecuteNonQuery();

        return destino;
    }

    /// <summary>Recusa arquivo que não seja um banco do Pingo OS, para não trocar o banco por qualquer coisa.</summary>
    public static bool EhBancoValido(string caminho, out string problema)
    {
        problema = "";
        try
        {
            using var conexao = new SqliteConnection($"Data Source={caminho};Mode=ReadOnly");
            conexao.Open();

            using var comando = conexao.CreateCommand();
            comando.CommandText =
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN " +
                "('OrdensServico','Clientes','Usuarios','__EFMigrationsHistory');";

            var tabelas = Convert.ToInt32(comando.ExecuteScalar());
            if (tabelas < 4)
            {
                problema = "O arquivo é um banco SQLite, mas não parece ser um backup do Pingo OS.";
                return false;
            }
            return true;
        }
        catch (SqliteException)
        {
            problema = "O arquivo não é um banco SQLite válido.";
            return false;
        }
    }

    /// <summary>
    /// Substitui o banco em uso. Antes disso derruba o pool de conexões do SQLite —
    /// sem isso o arquivo fica travado e a troca falha no Windows.
    /// </summary>
    public static void Restaurar(AppDbContext context, string origem)
    {
        var atual = CaminhoDoBanco(context);

        context.Database.CloseConnection();
        SqliteConnection.ClearAllPools();

        // Guarda o que estava antes: se a restauração for um engano, o arquivo continua lá
        var seguranca = atual + $".antes-da-restauracao-{DateTime.Now:yyyyMMdd-HHmmss}";
        if (File.Exists(atual)) File.Copy(atual, seguranca, overwrite: true);

        // O WAL e o SHM pertencem ao banco antigo; deixá-los corromperia o restaurado
        foreach (var sufixo in new[] { "-wal", "-shm" })
        {
            var arquivo = atual + sufixo;
            if (File.Exists(arquivo)) File.Delete(arquivo);
        }

        File.Copy(origem, atual, overwrite: true);
    }
}
