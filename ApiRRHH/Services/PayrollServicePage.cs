using ApiRRHH.Models;
using Microsoft.Data.SqlClient;

namespace ApiRRHH.Services
{
    public class PayrollServicePage
    {
        private readonly IConfiguration _configuration;

        public PayrollServicePage(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<UltimaPlanillaResponse?> ObtenerUltimaPlanillaPorDpiAsync(string dpi)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT TOP 1
                    E.CodigoEmpleado,
                    E.DPI,
                    P.PeriodoInicio,
                    P.PeriodoFin,
                    P.SueldoBase,
                    P.TotalIngresos,
                    P.TotalDescuentos,
                    P.LiquidoRecibir
                FROM Empleados E
                INNER JOIN Planillas P
                    ON E.IdEmpleado = P.IdEmpleado
                WHERE E.DPI = @DPI
                ORDER BY P.PeriodoFin DESC, P.IdPlanilla DESC;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DPI", dpi);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new UltimaPlanillaResponse
            {
                CodigoEmpleado = reader["CodigoEmpleado"]?.ToString() ?? "",
                DPI = reader["DPI"]?.ToString() ?? "",
                PeriodoInicio = reader["PeriodoInicio"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodoInicio"]),
                PeriodoFin = reader["PeriodoFin"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodoFin"]),
                SueldoBase = reader["SueldoBase"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SueldoBase"]),
                TotalIngresos = reader["TotalIngresos"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalIngresos"]),
                TotalDescuentos = reader["TotalDescuentos"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalDescuentos"]),
                LiquidoRecibir = reader["LiquidoRecibir"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LiquidoRecibir"])
            };
        }
    }
}