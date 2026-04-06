using ApiRRHH.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ApiRRHH.Services
{
    public class PayrollServicePage
    {
        private readonly IConfiguration _configuration;
        private readonly BitacoraService _bitacoraService;

        public PayrollServicePage(IConfiguration configuration, BitacoraService bitacoraService)
        {
            _configuration = configuration;
            _bitacoraService = bitacoraService;
        }

        // =============================
        // ✅ ÚLTIMA PLANILLA (YA EXISTENTE)
        // =============================
        public async Task<UltimaPlanillaResponse?> ObtenerUltimaPlanillaPorDpiAsync(string dpi)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT TOP 1
                    E.IdEmpleado,
                    E.CodigoEmpleado,
                    E.DPI,
                    P.PeriodoInicio,
                    P.PeriodoFin,
                    P.DiasTrabajados,
                    P.HorasExtras,
                    P.SueldoBase,
                    P.BonificacionDecreto,
                    P.SueldoExtraordinario,
                    P.TotalIngresos,
                    P.CuotaIGSS,
                    P.OtrosDescuentos,
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
            {
                await _bitacoraService.RegistrarBitacora(
                    null,
                    $"Consulta de última planilla por DPI: {dpi}",
                    "No se encontraron registros"
                );

                return null;
            }

            int idEmpleado = reader["IdEmpleado"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IdEmpleado"]);

            var response = new UltimaPlanillaResponse
            {
                CodigoEmpleado = reader["CodigoEmpleado"]?.ToString() ?? "",
                DPI = reader["DPI"]?.ToString() ?? "",
                PeriodoInicio = reader["PeriodoInicio"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodoInicio"]),
                PeriodoFin = reader["PeriodoFin"] == DBNull.Value ? null : Convert.ToDateTime(reader["PeriodoFin"]),
                DiasTrabajados = reader["DiasTrabajados"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DiasTrabajados"]),
                HorasExtras = reader["HorasExtras"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["HorasExtras"]),
                SueldoBase = reader["SueldoBase"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SueldoBase"]),
                BonificacionDecreto = reader["BonificacionDecreto"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["BonificacionDecreto"]),
                SueldoExtraordinario = reader["SueldoExtraordinario"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["SueldoExtraordinario"]),
                TotalIngresos = reader["TotalIngresos"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalIngresos"]),
                CuotaIGSS = reader["CuotaIGSS"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CuotaIGSS"]),
                OtrosDescuentos = reader["OtrosDescuentos"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["OtrosDescuentos"]),
                TotalDescuentos = reader["TotalDescuentos"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalDescuentos"]),
                LiquidoRecibir = reader["LiquidoRecibir"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["LiquidoRecibir"])
            };

            await _bitacoraService.RegistrarBitacora(
                idEmpleado,
                $"Consulta de última planilla por DPI: {dpi}",
                $"Consulta exitosa. Líquido a recibir: {response.LiquidoRecibir:N2}"
            );

            return response;
        }

        // =============================
        // 🆕 LISTAR PERIODOS
        // =============================
        public async Task<List<PeriodoPlanillaResponse>> ObtenerPeriodosPorDpiAsync(string dpi)
        {
            var lista = new List<PeriodoPlanillaResponse>();
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT 
                    P.PeriodoInicio,
                    P.PeriodoFin
                FROM Empleados E
                INNER JOIN Planillas P ON E.IdEmpleado = P.IdEmpleado
                WHERE E.DPI = @DPI
                ORDER BY P.PeriodoFin DESC;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DPI", dpi);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                DateTime? inicio = reader["PeriodoInicio"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["PeriodoInicio"]);

                DateTime? fin = reader["PeriodoFin"] == DBNull.Value
                    ? (DateTime?)null
                    : Convert.ToDateTime(reader["PeriodoFin"]);

                lista.Add(new PeriodoPlanillaResponse
                {
                    PeriodoInicio = inicio,
                    PeriodoFin = fin,
                    Descripcion = FormatearDescripcionPeriodo(inicio, fin)
                });
            }

            return lista;
        }

        // =============================
        // 🆕 CONSULTAR POR PERIODO
        // =============================
        public async Task<UltimaPlanillaResponse?> ObtenerPlanillaPorPeriodoAsync(string dpi, DateTime inicio, DateTime fin)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT 
                    E.IdEmpleado,
                    E.CodigoEmpleado,
                    E.DPI,
                    P.PeriodoInicio,
                    P.PeriodoFin,
                    P.DiasTrabajados,
                    P.HorasExtras,
                    P.SueldoBase,
                    P.BonificacionDecreto,
                    P.SueldoExtraordinario,
                    P.TotalIngresos,
                    P.CuotaIGSS,
                    P.OtrosDescuentos,
                    P.TotalDescuentos,
                    P.LiquidoRecibir
                FROM Empleados E
                INNER JOIN Planillas P ON E.IdEmpleado = P.IdEmpleado
                WHERE E.DPI = @DPI
                  AND P.PeriodoInicio = @Inicio
                  AND P.PeriodoFin = @Fin;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DPI", dpi);
            command.Parameters.AddWithValue("@Inicio", inicio);
            command.Parameters.AddWithValue("@Fin", fin);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return new UltimaPlanillaResponse
            {
                CodigoEmpleado = reader["CodigoEmpleado"]?.ToString() ?? "",
                DPI = reader["DPI"]?.ToString() ?? "",
                PeriodoInicio = Convert.ToDateTime(reader["PeriodoInicio"]),
                PeriodoFin = Convert.ToDateTime(reader["PeriodoFin"]),
                DiasTrabajados = Convert.ToDecimal(reader["DiasTrabajados"]),
                HorasExtras = Convert.ToDecimal(reader["HorasExtras"]),
                SueldoBase = Convert.ToDecimal(reader["SueldoBase"]),
                BonificacionDecreto = Convert.ToDecimal(reader["BonificacionDecreto"]),
                SueldoExtraordinario = Convert.ToDecimal(reader["SueldoExtraordinario"]),
                TotalIngresos = Convert.ToDecimal(reader["TotalIngresos"]),
                CuotaIGSS = Convert.ToDecimal(reader["CuotaIGSS"]),
                OtrosDescuentos = Convert.ToDecimal(reader["OtrosDescuentos"]),
                TotalDescuentos = Convert.ToDecimal(reader["TotalDescuentos"]),
                LiquidoRecibir = Convert.ToDecimal(reader["LiquidoRecibir"])
            };
        }

        // =============================
        // 🧠 FORMATEAR TEXTO
        // =============================
        private string FormatearDescripcionPeriodo(DateTime? inicio, DateTime? fin)
        {
            if (inicio == null || fin == null) return "";

            var mes = inicio.Value.ToString("MMMM");
            var anio = inicio.Value.Year;

            if (inicio.Value.Day <= 15)
                return $"Primera quincena de {mes} {anio}";
            else
                return $"Segunda quincena de {mes} {anio}";
        }
    }
}