using Microsoft.Data.SqlClient;
using System.Data;

namespace ApiRRHH.Services;

public class BitacoraService
{
    private readonly string _connectionString;

    public BitacoraService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task RegistrarBitacora(int? idEmpleado, string consulta, string resultado)
    {
        await using SqlConnection conn = new SqlConnection(_connectionString);

        const string sql = @"
            INSERT INTO BitacoraConsultas (IdEmpleado, FechaHora, Consulta, Resultado)
            VALUES (@IdEmpleado, GETDATE(), @Consulta, @Resultado);";

        await using SqlCommand cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IdEmpleado", (object?)idEmpleado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Consulta", consulta);
        cmd.Parameters.AddWithValue("@Resultado", resultado);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}