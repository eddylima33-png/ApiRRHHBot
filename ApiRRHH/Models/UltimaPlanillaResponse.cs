namespace ApiRRHH.Models
{
    public class UltimaPlanillaResponse
    {
        public string CodigoEmpleado { get; set; } = "";
        public string DPI { get; set; } = "";
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFin { get; set; }
        public decimal SueldoBase { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal LiquidoRecibir { get; set; }
    }
}