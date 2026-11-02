namespace VITALIA.Models.ViewModels;

/// <summary>Tarjeta de un residente en el inicio del familiar.</summary>
public class FamiliarResidenteTarjeta
{
    public int IdResidente { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string CodigoUnico { get; set; } = string.Empty;
    public int Edad { get; set; }
    public string Habitacion { get; set; } = string.Empty;
    public string Parentesco { get; set; } = string.Empty;
    public List<string> Equipo { get; set; } = new();
    public int AlertasActivas { get; set; }
    public DateTime? UltimaAtencion { get; set; }
}

/// <summary>Ficha de solo lectura que ve el familiar.</summary>
public class FamiliarFichaViewModel
{
    public Residente Residente { get; set; } = null!;
    public int Edad { get; set; }
    public string Parentesco { get; set; } = string.Empty;
    public List<FamiliarEquipoItem> Equipo { get; set; } = new();
    public List<FamiliarAtencionItem> Atenciones { get; set; } = new();
    public List<FamiliarAlertaItem> AlertasActivas { get; set; } = new();
    public List<FamiliarAlertaItem> AlertasAtendidas { get; set; } = new();
    public List<FamiliarMedicamentoItem> Medicamentos { get; set; } = new();
    // false si la base todavía no tiene las tablas del módulo Farmacia
    public bool MedicamentosDisponibles { get; set; } = true;
}

/// <summary>Medicamento entregado al residente (módulo Farmacia).</summary>
public class FamiliarMedicamentoItem
{
    public DateTime FechaHora { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string? Presentacion { get; set; }
    public int Cantidad { get; set; }
    public string EntregadoPor { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

public class FamiliarEquipoItem
{
    public string Nombre { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateTime Desde { get; set; }
}

public class FamiliarAtencionItem
{
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public string AtendidoPor { get; set; } = string.Empty;
    public string? MotivoCorreccion { get; set; }
}

public class FamiliarAlertaItem
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Responsable { get; set; }
}

public static class Edades
{
    public static int Calcular(DateOnly nacimiento)
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        int edad = hoy.Year - nacimiento.Year;
        if (nacimiento > hoy.AddYears(-edad)) edad--;
        return edad;
    }
}
