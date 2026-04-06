namespace ApiRRHH.Models
{
    public class UltimaPlanillaResponse
    {
        public string CodigoEmpleado { get; set; } = "";
        public string DPI { get; set; } = "";
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFin { get; set; }

        public decimal DiasTrabajados { get; set; }
        public decimal HorasExtras { get; set; }
        public decimal SueldoBase { get; set; }
        public decimal BonificacionDecreto { get; set; }
        public decimal SueldoExtraordinario { get; set; }
        public decimal Vacaciones { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal CuotaIGSS { get; set; }
        public decimal OtrosDescuentos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal LiquidoRecibir { get; set; }
    }
}