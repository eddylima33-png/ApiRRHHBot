using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace ApiRRHH.Services
{
    public class PortalAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly BitacoraService _bitacoraService;

        public PortalAuthService(IConfiguration configuration, BitacoraService bitacoraService)
        {
            _configuration = configuration;
            _bitacoraService = bitacoraService;
        }

        public async Task<(bool ExisteEmpleado, bool TienePerfil, int IdEmpleado, string CodigoEmpleado)> VerificarEstadoDpiAsync(string dpi)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT TOP 1
                    E.IdEmpleado,
                    E.CodigoEmpleado,
                    CASE 
                        WHEN A.IdAcceso IS NULL THEN 0
                        ELSE 1
                    END AS TienePerfil
                FROM Empleados E
                LEFT JOIN AccesoPortalEmpleados A
                    ON E.IdEmpleado = A.IdEmpleado
                    AND A.Activo = 1
                WHERE E.DPI = @DPI;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DPI", dpi);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await _bitacoraService.RegistrarBitacora(
                    null,
                    $"VALIDACION_DPI_PORTAL: {dpi}",
                    "DPI no encontrado"
                );

                return (false, false, 0, "");
            }

            int idEmpleado = reader["IdEmpleado"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IdEmpleado"]);
            string codigoEmpleado = reader["CodigoEmpleado"]?.ToString() ?? "";
            bool tienePerfil = reader["TienePerfil"] != DBNull.Value && Convert.ToInt32(reader["TienePerfil"]) == 1;

            await _bitacoraService.RegistrarBitacora(
                idEmpleado,
                $"VALIDACION_DPI_PORTAL: {dpi}",
                tienePerfil ? "Empleado encontrado con perfil" : "Empleado encontrado sin perfil"
            );

            return (true, tienePerfil, idEmpleado, codigoEmpleado);
        }

        public async Task<(bool Ok, string Mensaje)> RegistrarPinAsync(string dpi, string pin)
        {
            if (string.IsNullOrWhiteSpace(dpi))
                return (false, "Debe ingresar un DPI.");

            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4 || !pin.All(char.IsDigit))
                return (false, "El PIN debe tener exactamente 4 dígitos.");

            var estado = await VerificarEstadoDpiAsync(dpi);

            if (!estado.ExisteEmpleado)
                return (false, "El DPI no existe en el sistema.");

            if (estado.TienePerfil)
                return (false, "Este empleado ya tiene un perfil creado.");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            string pinHash = GenerarHash(pin);

            const string sql = @"
                INSERT INTO AccesoPortalEmpleados
                (
                    IdEmpleado,
                    DPI,
                    PinHash,
                    FechaCreacion,
                    UltimoAcceso,
                    Activo,
                    IntentosFallidos,
                    Bloqueado
                )
                VALUES
                (
                    @IdEmpleado,
                    @DPI,
                    @PinHash,
                    GETDATE(),
                    NULL,
                    1,
                    0,
                    0
                );";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEmpleado", estado.IdEmpleado);
            command.Parameters.AddWithValue("@DPI", dpi);
            command.Parameters.AddWithValue("@PinHash", pinHash);

            int filas = await command.ExecuteNonQueryAsync();

            if (filas > 0)
            {
                await _bitacoraService.RegistrarBitacora(
                    estado.IdEmpleado,
                    $"CREACION_PIN_PORTAL: {dpi}",
                    "Perfil creado correctamente"
                );

                return (true, "PIN registrado correctamente.");
            }

            await _bitacoraService.RegistrarBitacora(
                estado.IdEmpleado,
                $"CREACION_PIN_PORTAL: {dpi}",
                "No fue posible crear el perfil"
            );

            return (false, "No fue posible registrar el PIN.");
        }

        public async Task<(bool Ok, string Mensaje, int IdEmpleado, string CodigoEmpleado)> LoginAsync(string dpi, string pin)
        {
            if (string.IsNullOrWhiteSpace(dpi))
                return (false, "Debe ingresar un DPI.", 0, "");

            if (string.IsNullOrWhiteSpace(pin))
                return (false, "Debe ingresar su PIN.", 0, "");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                SELECT TOP 1
                    E.IdEmpleado,
                    E.CodigoEmpleado,
                    A.PinHash,
                    A.IntentosFallidos,
                    A.Bloqueado,
                    A.Activo
                FROM Empleados E
                INNER JOIN AccesoPortalEmpleados A
                    ON E.IdEmpleado = A.IdEmpleado
                WHERE E.DPI = @DPI;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DPI", dpi);

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                await _bitacoraService.RegistrarBitacora(
                    null,
                    $"LOGIN_PORTAL: {dpi}",
                    "No existe perfil registrado"
                );

                return (false, "No existe perfil registrado para este DPI.", 0, "");
            }

            int idEmpleado = reader["IdEmpleado"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IdEmpleado"]);
            string codigoEmpleado = reader["CodigoEmpleado"]?.ToString() ?? "";
            string pinHashGuardado = reader["PinHash"]?.ToString() ?? "";
            bool bloqueado = reader["Bloqueado"] != DBNull.Value && Convert.ToBoolean(reader["Bloqueado"]);
            bool activo = reader["Activo"] != DBNull.Value && Convert.ToBoolean(reader["Activo"]);
            int intentosFallidos = reader["IntentosFallidos"] == DBNull.Value ? 0 : Convert.ToInt32(reader["IntentosFallidos"]);

            await reader.CloseAsync();

            if (!activo)
            {
                await _bitacoraService.RegistrarBitacora(
                    idEmpleado,
                    $"LOGIN_PORTAL: {dpi}",
                    "Perfil inactivo"
                );

                return (false, "El perfil se encuentra inactivo.", idEmpleado, codigoEmpleado);
            }

            if (bloqueado)
            {
                await _bitacoraService.RegistrarBitacora(
                    idEmpleado,
                    $"LOGIN_PORTAL: {dpi}",
                    "Perfil bloqueado"
                );

                return (false, "El perfil se encuentra bloqueado.", idEmpleado, codigoEmpleado);
            }

            string pinHashIngresado = GenerarHash(pin);

            if (pinHashIngresado != pinHashGuardado)
            {
                await AumentarIntentosFallidosAsync(idEmpleado, intentosFallidos + 1);

                await _bitacoraService.RegistrarBitacora(
                    idEmpleado,
                    $"LOGIN_PORTAL: {dpi}",
                    "PIN incorrecto"
                );

                return (false, "PIN incorrecto.", idEmpleado, codigoEmpleado);
            }

            await ReiniciarIntentosYActualizarAccesoAsync(idEmpleado);

            await _bitacoraService.RegistrarBitacora(
                idEmpleado,
                $"LOGIN_PORTAL: {dpi}",
                "Acceso correcto"
            );

            return (true, "Ingreso correcto.", idEmpleado, codigoEmpleado);
        }

        private async Task AumentarIntentosFallidosAsync(int idEmpleado, int nuevoIntento)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                UPDATE AccesoPortalEmpleados
                SET IntentosFallidos = @IntentosFallidos,
                    Bloqueado = CASE WHEN @IntentosFallidos >= 3 THEN 1 ELSE Bloqueado END
                WHERE IdEmpleado = @IdEmpleado;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IntentosFallidos", nuevoIntento);
            command.Parameters.AddWithValue("@IdEmpleado", idEmpleado);

            await command.ExecuteNonQueryAsync();
        }

        private async Task ReiniciarIntentosYActualizarAccesoAsync(int idEmpleado)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            const string sql = @"
                UPDATE AccesoPortalEmpleados
                SET IntentosFallidos = 0,
                    UltimoAcceso = GETDATE()
                WHERE IdEmpleado = @IdEmpleado;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdEmpleado", idEmpleado);

            await command.ExecuteNonQueryAsync();
        }

        private string GenerarHash(string texto)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(texto));
            return Convert.ToBase64String(bytes);
        }
    }
}