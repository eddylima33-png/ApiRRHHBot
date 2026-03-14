using Microsoft.Data.SqlClient;
using System.Data;

namespace ApiRRHH.Services;

public class PayrollService
{
    private readonly string _connectionString;

    public PayrollService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RRHHBot")!;
    }

    public async Task<(bool exists, int idEmpleado, string nombre)> ValidateEmployeeAsync(string codigoEmpleado)
    {
        const string sql = @"
            SELECT TOP 1 IdEmpleado, PrimerNombre, PrimerApellido
            FROM Empleados
            WHERE CodigoEmpleado = @codigo
              AND Estado = 'Activo';";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@codigo", codigoEmpleado);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var nombre = $"{reader["PrimerNombre"]} {reader["PrimerApellido"]}";
            return (true, id, nombre);
        }

        return (false, 0, "");
    }

    public async Task<string?> GetLatestPayrollSummaryAsync(int idEmpleado)
    {
        const string sql = @"
            SELECT TOP 1
                PeriodoInicio,
                PeriodoFin,
                TotalIngresos,
                TotalDescuentos,
                LiquidoRecibir
            FROM Planillas
            WHERE IdEmpleado = @idEmpleado
            ORDER BY PeriodoFin DESC, IdPlanilla DESC;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idEmpleado", idEmpleado);

        await using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var inicio = Convert.ToDateTime(reader["PeriodoInicio"]).ToString("dd/MM/yyyy");
            var fin = Convert.ToDateTime(reader["PeriodoFin"]).ToString("dd/MM/yyyy");
            var ingresos = Convert.ToDecimal(reader["TotalIngresos"]).ToString("N2");
            var descuentos = Convert.ToDecimal(reader["TotalDescuentos"]).ToString("N2");
            var liquido = Convert.ToDecimal(reader["LiquidoRecibir"]).ToString("N2");

            return $"Última planilla:\nPeríodo: {inicio} al {fin}\nIngresos: Q{ingresos}\nDescuentos: Q{descuentos}\nLíquido: Q{liquido}";
        }

        return null;
    }
}

