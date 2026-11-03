using Microsoft.EntityFrameworkCore;
using VITALIA.Models;

namespace VITALIA.Data;

/// <summary>Lo que se cargó o se quitó con los datos de presentación.</summary>
public record ResultadoPresentacion(
    int Residentes, int Personal, int Familiares, int Medicamentos,
    int Atenciones, int Alertas, int Entregas, List<string> Avisos);

/// <summary>
/// Datos de presentación de VITALIA: residentes, personal, familiares, farmacia, alertas y bitácora
/// con información realista, iguales para todo el grupo.
/// Se cargan desde el panel del administrador: no duplican nada y no tocan los datos que ya existen.
/// Las fechas se arman a partir del momento en que se cargan, para que todo se vea reciente.
/// </summary>
public static class DatosPresentacion
{
    private const int DiasBitacora = 14;
    private const int DiasEntregas = 7;

    // ---------------------------------------------------------------- personal

    private record PersonalDato(string Correo, string Nombre, string Apellido, string Cargo, string Rol, bool Activo = true);

    // 0-1 médicos, 2-4 enfermería, 5-8 cuidadores, 9 dirección, 10 cuidadora que ya no trabaja en el hogar
    private static readonly PersonalDato[] Personal =
    {
        new("ricardo.menjivar@vitalia.com", "Ricardo Alberto", "Menjívar Castro", "Médico general", RolesSistema.Medico),
        new("patricia.villalta@vitalia.com", "Patricia Eugenia", "Villalta Romero", "Médica geriatra", RolesSistema.Medico),
        new("sandra.alvarado@vitalia.com", "Sandra Beatriz", "Alvarado Mena", "Enfermera jefe", RolesSistema.Enfermero),
        new("marvin.orellana@vitalia.com", "Marvin Ernesto", "Orellana Díaz", "Enfermero", RolesSistema.Enfermero),
        new("gloria.campos@vitalia.com", "Gloria Esperanza", "Campos Rivera", "Enfermera", RolesSistema.Enfermero),
        new("jorge.aguilar@vitalia.com", "Jorge Luis", "Aguilar Peña", "Cuidador", RolesSistema.Enfermero),
        new("rosa.escobar@vitalia.com", "Rosa Delmy", "Escobar Lozano", "Cuidadora", RolesSistema.Enfermero),
        new("wilfredo.cruz@vitalia.com", "Wilfredo Antonio", "Cruz Mejía", "Cuidador", RolesSistema.Enfermero),
        new("xiomara.henriquez@vitalia.com", "Xiomara Lisseth", "Henríquez Solís", "Cuidadora", RolesSistema.Enfermero),
        new("claudia.arevalo@vitalia.com", "Claudia Marisol", "Arévalo Duarte", "Directora administrativa", RolesSistema.Administrador),
        new("alicia.benitez@vitalia.com", "Alicia Margarita", "Benítez Galdámez", "Cuidadora", RolesSistema.Enfermero, Activo: false)
    };

    private const int Direccion = 9;
    private const int CuidadoraAnterior = 10;
    private const int DiasCambioCuidador = 120;

    /// <summary>Cuenta que se crea con los datos: si existe, los datos ya se cargaron.</summary>
    private const string CorreoMarcador = "claudia.arevalo@vitalia.com";
    private static readonly int[] Enfermeria = { 2, 3, 4 };

    // ---------------------------------------------------------------- medicamentos

    private record LoteDato(string Numero, int DiasParaVencer, int Stock);
    private record MedicamentoDato(string Codigo, string Nombre, string Presentacion, string Descripcion, string Dosis, LoteDato[] Lotes, bool Activo = true);

    private static readonly MedicamentoDato[] Medicamentos =
    {
        new("MED-101", "Losartán", "Tableta 50 mg", "Antihipertensivo", "50 mg cada 24 horas",
            new[] { new LoteDato("LOS-2611-A", 420, 120), new LoteDato("LOS-2603-B", 150, 40) }),
        new("MED-102", "Metformina", "Tableta 850 mg", "Control de la glucosa en diabetes tipo 2", "850 mg con el desayuno y la cena",
            new[] { new LoteDato("MET-2612-A", 390, 95) }),
        new("MED-103", "Amlodipino", "Tableta 5 mg", "Antihipertensivo", "5 mg cada 24 horas",
            new[] { new LoteDato("AML-2609-A", 310, 60) }),
        new("MED-104", "Omeprazol", "Cápsula 20 mg", "Protector gástrico", "20 mg en ayunas",
            new[] { new LoteDato("OME-2701-A", 450, 80) }),
        new("MED-105", "Atorvastatina", "Tableta 20 mg", "Control del colesterol", "20 mg por la noche",
            new[] { new LoteDato("ATO-2605-A", 200, 70), new LoteDato("ATO-2509-V", -25, 6) }),
        new("MED-106", "Levotiroxina", "Tableta 100 mcg", "Tratamiento del hipotiroidismo", "100 mcg en ayunas",
            new[] { new LoteDato("LEV-2608-A", 280, 45) }),
        new("MED-107", "Paracetamol", "Tableta 500 mg", "Analgésico y antipirético", "500 mg cada 8 horas si hay dolor",
            new[] { new LoteDato("PAR-2511-A", 20, 35), new LoteDato("PAR-2706-B", 600, 150) }),
        new("MED-108", "Ácido acetilsalicílico", "Tableta 100 mg", "Antiagregante plaquetario", "100 mg con el almuerzo",
            new[] { new LoteDato("ASA-2610-A", 340, 90) }),
        new("MED-109", "Furosemida", "Tableta 40 mg", "Diurético", "40 mg por la mañana",
            new[] { new LoteDato("FUR-2607-A", 250, 30) }),
        new("MED-110", "Donepezilo", "Tableta 10 mg", "Tratamiento de la demencia tipo Alzheimer", "10 mg por la noche",
            new[] { new LoteDato("DON-2604-A", 160, 9) }),
        new("MED-111", "Insulina NPH", "Frasco 100 UI/ml", "Insulina de acción intermedia", "12 UI antes del desayuno",
            new[] { new LoteDato("INS-2603-A", 130, 4) }),
        new("MED-112", "Calcio + Vitamina D3", "Tableta 600 mg/400 UI", "Suplemento para la osteoporosis", "1 tableta con el almuerzo",
            new[] { new LoteDato("CAL-2702-A", 480, 110) }),
        new("MED-113", "Salbutamol", "Inhalador 100 mcg", "Broncodilatador", "2 inhalaciones cada 6 horas si hay falta de aire",
            new[] { new LoteDato("SAL-2606-A", 230, 3) }),
        new("MED-114", "Levodopa/Carbidopa", "Tableta 250/25 mg", "Tratamiento de la enfermedad de Parkinson", "1 tableta cada 8 horas",
            new[] { new LoteDato("LDC-2609-A", 300, 75) }),
        new("MED-115", "Ibuprofeno", "Tableta 400 mg", "Antiinflamatorio", "400 mg cada 8 horas si hay dolor",
            new[] { new LoteDato("IBU-2604-A", 170, 0) }),
        new("MED-116", "Ranitidina", "Tableta 150 mg", "Protector gástrico (descontinuado; se cambió por omeprazol)", "150 mg cada 12 horas",
            new[] { new LoteDato("RAN-2507-A", -95, 0) }, Activo: false)
    };

    // ---------------------------------------------------------------- residentes

    private record ResidenteDato(
        string Codigo, string Nombre, string Apellido, DateOnly Nacimiento, string Habitacion,
        string? Sangre, string? Alergias, string Contacto, string Antecedentes,
        string[] Medicamentos, int Medico, int Cuidador, int DiasIngreso, string Condicion,
        int DiasBaja = 0, bool CambioDeCuidador = false);

    private static readonly ResidenteDato[] Residentes =
    {
        new("RES-2026-101", "María Luisa", "Barrera de Molina", new DateOnly(1938, 4, 12), "101", "O+", "Penicilina",
            "Sonia Patricia Molina · 7845-2210",
            "Hipertensión arterial controlada. Usa bastón para caminar distancias largas.",
            new[] { "MED-101", "MED-112" }, 0, 5, 520, "hipertension", CambioDeCuidador: true),
        new("RES-2026-102", "José Ernesto", "Guardado Méndez", new DateOnly(1941, 9, 3), "102", "A+", null,
            "Ernesto Alonso Guardado · 7612-9034",
            "Diabetes mellitus tipo 2 e hipertensión arterial. Dieta baja en azúcar y sal.",
            new[] { "MED-102", "MED-101" }, 1, 6, 410, "diabetes"),
        new("RES-2026-103", "Ana Cecilia", "Rodríguez Zelaya", new DateOnly(1945, 1, 27), "103", "B+", "Sulfas",
            "Lucía Fernanda Rodríguez · 7290-4417",
            "Alzheimer en etapa inicial. Requiere supervisión para la medicación y acompañamiento al caminar.",
            new[] { "MED-110" }, 1, 7, 300, "demencia"),
        new("RES-2026-104", "Francisco Javier", "Mancía Rosales", new DateOnly(1936, 11, 18), "104", "O-", "Mariscos",
            "Roberto Mancía · 7533-0921",
            "EPOC por exposición a humo de leña. Usa inhalador de rescate y oxígeno por las noches.",
            new[] { "MED-113" }, 0, 8, 610, "epoc", CambioDeCuidador: true),
        new("RES-2026-105", "Blanca Rosa", "Fuentes de Linares", new DateOnly(1943, 6, 5), "105", "A-", null,
            "Ricardo Linares Fuentes · 7108-5576",
            "Artrosis de rodilla bilateral. Terapia física tres veces por semana.",
            new[] { "MED-107", "MED-112" }, 1, 5, 270, "artrosis"),
        new("RES-2026-106", "Héctor Manuel", "Serrano Bonilla", new DateOnly(1939, 2, 14), "106", "O+", "Ácido acetilsalicílico",
            "Diego Alejandro Serrano · 7034-8865",
            "Insuficiencia cardíaca leve. Control diario de peso y de líquidos.",
            new[] { "MED-109", "MED-105" }, 0, 6, 480, "cardiaco"),
        new("RES-2026-107", "Teresa de Jesús", "Alfaro Cañas", new DateOnly(1934, 12, 8), "107", "AB+", null,
            "Julia Alfaro de Paz · 2264-7310",
            "Hipotiroidismo. Pérdida auditiva moderada, usa audífono en el oído derecho.",
            new[] { "MED-106" }, 1, 7, 730, "tiroides", CambioDeCuidador: true),
        new("RES-2026-108", "Rafael Antonio", "Sosa Lemus", new DateOnly(1947, 7, 22), "108", "B-", "Polen",
            "Marta Alicia de Sosa · 2275-3190",
            "Secuelas de evento cerebrovascular (2023): debilidad del lado izquierdo. Se moviliza en silla de ruedas.",
            new[] { "MED-108", "MED-105" }, 0, 8, 350, "acv"),
        new("RES-2026-109", "Dolores Isabel", "Interiano de Chávez", new DateOnly(1940, 3, 30), "109", "O+", "Ibuprofeno",
            "Julio César Chávez · 7788-1023",
            "Diabetes tipo 2 insulinodependiente. Glucometría antes del desayuno y de la cena.",
            new[] { "MED-111", "MED-102" }, 1, 5, 560, "diabetes"),
        new("RES-2026-110", "Santiago Arturo", "Velásquez Rivera", new DateOnly(1944, 10, 11), "110", "A+", null,
            "Claudia Velásquez · 7642-8857",
            "Hipertensión y colesterol alto. Independiente para sus actividades diarias.",
            new[] { "MED-103", "MED-105" }, 0, 6, 200, "hipertension"),
        new("RES-2026-111", "Concepción del Carmen", "Ortiz Saravia", new DateOnly(1937, 5, 19), "111", "O+", "Látex",
            "Karen Vanessa Ortiz · 7156-6642",
            "Osteoporosis con antecedente de fractura de cadera (2024). Riesgo alto de caídas.",
            new[] { "MED-112", "MED-107" }, 1, 7, 440, "osteoporosis"),
        new("RES-2026-112", "Óscar Armando", "Turcios Lara", new DateOnly(1949, 8, 25), "112", "B+", null,
            "Andrea Beatriz Turcios · 7921-5508",
            "Enfermedad de Parkinson. Temblor en reposo; necesita ayuda para vestirse.",
            new[] { "MED-114" }, 0, 8, 330, "parkinson"),
        new("RES-2026-113", "Hilda Marina", "Carranza de Funes", new DateOnly(1942, 1, 9), "113", "A+", "Penicilina",
            "Raúl Funes Carranza · 7380-2264",
            "Gastritis crónica e hipertensión arterial. Dieta blanda.",
            new[] { "MED-104", "MED-101" }, 1, 5, 390, "gastritis"),
        new("RES-2026-114", "Lorenzo Antonio", "Recinos Galdámez", new DateOnly(1935, 6, 29), "114", "O-", null,
            "Mauricio Recinos · 7467-3381",
            "Demencia vascular moderada. Desorientación por las tardes; acompañamiento permanente.",
            new[] { "MED-110", "MED-108" }, 0, 6, 650, "demencia"),
        new("RES-2026-115", "Gladys Noemí", "Avelar Sandoval", new DateOnly(1946, 4, 2), "115", "AB-", "Nueces",
            "Elsy Sandoval · 7219-4430",
            "Ingresó hace una semana. Hipertensión leve; en periodo de adaptación.",
            new[] { "MED-103" }, 1, 7, 7, "hipertension"),
        new("RES-2026-116", "Ernesto Benjamín", "Palacios Ruiz", new DateOnly(1933, 8, 14), "116", "A+", null,
            "Tomás Palacios · 7556-1180",
            "Hipertensión arterial y cataratas. Dado de baja: se trasladó a vivir con su familia en Santa Ana.",
            new[] { "MED-101" }, 0, 7, 260, "hipertension", DiasBaja: 45)
    };

    // ---------------------------------------------------------------- familiares

    private record FamiliarDato(string Correo, string Codigo, string Parentesco, string Telefono);

    private static readonly FamiliarDato[] Familiares =
    {
        new("sonia.molina@gmail.com", "RES-2026-101", "Hija", "7845-2210"),
        new("ernesto.guardado@hotmail.com", "RES-2026-102", "Hijo", "7612-9034"),
        new("gabriela.guardado@gmail.com", "RES-2026-102", "Nieta", "7311-2096"),
        new("lucia.rodriguez.z@gmail.com", "RES-2026-103", "Hija", "7290-4417"),
        new("diego.serrano@gmail.com", "RES-2026-106", "Nieto", "7034-8865"),
        new("marta.sosa@yahoo.com", "RES-2026-108", "Esposa", "2275-3190"),
        new("julio.chavez@gmail.com", "RES-2026-109", "Hijo", "7788-1023"),
        new("karen.ortiz@gmail.com", "RES-2026-111", "Sobrina", "7156-6642"),
        new("andrea.turcios@outlook.com", "RES-2026-112", "Hija", "7921-5508"),
        new("mauricio.recinos@gmail.com", "RES-2026-114", "Hijo", "7467-3381")
    };

    // ---------------------------------------------------------------- tipos de atención

    private const string Alimentacion = "Alimentación";
    private const string Medicacion = "Medicación";
    private const string SignosVitales = "Signos vitales";
    private const string Higiene = "Higiene personal";
    private const string ActividadFisica = "Actividad física";
    private const string Curacion = "Curación";
    private const string Recreacion = "Recreación";
    private const string ControlMedico = "Control médico";

    private static readonly (string Nombre, string Descripcion)[] Tipos =
    {
        (Alimentacion, "Desayuno, almuerzo y cena"),
        (Medicacion, "Administración de medicamentos indicados"),
        (SignosVitales, "Presión, pulso, temperatura y saturación"),
        (Higiene, "Baño, aseo y cambio de ropa"),
        (ActividadFisica, "Caminatas, ejercicios y terapia física"),
        (Curacion, "Curaciones y cuidado de la piel"),
        (Recreacion, "Actividades recreativas y de convivencia"),
        (ControlMedico, "Revisión del médico")
    };

    // ================================================================ cuentas (para la pantalla del admin)

    /// <summary>Cuentas que se crean, para mostrarlas en la pantalla del administrador.</summary>
    public static IEnumerable<(string Correo, string Nombre, string Rol)> Cuentas() =>
        Personal.Select(p => (p.Correo, p.Nombre.Split(' ')[0] + " " + p.Apellido.Split(' ')[0] + " · " + p.Cargo +
                                     (p.Activo ? "" : " (ya no trabaja, cuenta desactivada)"), p.Rol))
            .Concat(Familiares.Select(f => (f.Correo,
                f.Parentesco + " de " + Residentes.First(r => r.Codigo == f.Codigo).Nombre.Split(' ')[0] + " " +
                Residentes.First(r => r.Codigo == f.Codigo).Apellido.Split(' ')[0], RolesSistema.Familiar)));

    /// <summary>True si los datos de presentación ya se cargaron en esta base (la opción ya no se muestra).</summary>
    public static Task<bool> CargadosAsync(VitaliaDbContext db) =>
        db.Usuarios.AnyAsync(u => u.Correo == CorreoMarcador);

    private static List<string> CorreosDelPaquete() =>
        Personal.Select(p => p.Correo).Concat(Familiares.Select(f => f.Correo)).ToList();

    // ================================================================ cargar

    public static async Task<ResultadoPresentacion> CargarAsync(VitaliaDbContext db, DateTime ahora)
    {
        await using var transaccion = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;
        var avisos = new List<string>();
        int nuevosPersonal = 0, nuevosFamiliares = 0, nuevosMedicamentos = 0, nuevosResidentes = 0;
        int atenciones = 0, alertas = 0, entregas = 0;

        // Se cargan una sola vez por base
        if (await CargadosAsync(db))
            return new ResultadoPresentacion(0, 0, 0, 0, 0, 0, 0, avisos);

        await DatosIniciales.CrearSiFaltanAsync(db);
        var roles = await db.Roles.ToDictionaryAsync(r => r.NombreRol, r => r.IdRol);

        // 1. Tipos de atención que falten (se comparan sin importar mayúsculas)
        var tipos = new Dictionary<string, int>();
        var tiposExistentes = await db.TipoAtencions.ToListAsync();
        foreach (var (nombre, descripcion) in Tipos)
        {
            var tipo = tiposExistentes.FirstOrDefault(t => string.Equals(t.NombreCategoria.Trim(), nombre, StringComparison.OrdinalIgnoreCase));
            if (tipo == null)
            {
                tipo = new TipoAtencion { NombreCategoria = nombre, Descripcion = descripcion };
                db.TipoAtencions.Add(tipo);
                await db.SaveChangesAsync();
            }
            tipos[nombre] = tipo.IdTipoAtencion;
        }

        // 2. Personal (cuenta + empleado)
        var usuarios = new int[Personal.Length];
        var empleados = new int[Personal.Length];
        for (int i = 0; i < Personal.Length; i++)
        {
            var p = Personal[i];
            var usuario = await db.Usuarios.Include(u => u.Empleado).FirstOrDefaultAsync(u => u.Correo == p.Correo);
            if (usuario == null)
            {
                usuario = new Usuario { Correo = p.Correo, IdRol = roles[p.Rol], ContrasenaHash = DatosIniciales.ContrasenaPorDefectoHash, Estado = p.Activo };
                db.Usuarios.Add(usuario);
                await db.SaveChangesAsync();
                nuevosPersonal++;
            }
            var empleado = usuario.Empleado ?? await db.Empleados.FirstOrDefaultAsync(e => e.IdUsuario == usuario.IdUsuario);
            if (empleado == null)
            {
                empleado = new Empleado { IdUsuario = usuario.IdUsuario, Nombre = p.Nombre, Apellido = p.Apellido, Cargo = p.Cargo, Estado = p.Activo };
                db.Empleados.Add(empleado);
                await db.SaveChangesAsync();
            }
            usuarios[i] = usuario.IdUsuario;
            empleados[i] = empleado.IdEmpleado;
        }

        // 3. Medicamentos con sus lotes; se guarda el lote del que salen las entregas
        var medicamentos = new Dictionary<string, (int Id, int Lote)>();
        foreach (var m in Medicamentos)
        {
            var existente = await db.Medicamentos.FirstOrDefaultAsync(x => x.Codigo == m.Codigo);
            if (existente != null && existente.Nombre != m.Nombre)
            {
                avisos.Add($"Ya existe otro medicamento con el código {m.Codigo} ({existente.Nombre}); no se cargó {m.Nombre}.");
                continue;
            }
            if (existente == null)
            {
                existente = new Medicamento { Codigo = m.Codigo, Nombre = m.Nombre, Presentacion = m.Presentacion, Descripcion = m.Descripcion, Estado = m.Activo };
                db.Medicamentos.Add(existente);
                await db.SaveChangesAsync();
                foreach (var l in m.Lotes)
                {
                    db.LoteMedicamentos.Add(new LoteMedicamento
                    {
                        IdMedicamento = existente.IdMedicamento,
                        NumeroLote = l.Numero,
                        FechaVencimiento = ahora.Date.AddDays(l.DiasParaVencer),
                        Stock = l.Stock
                    });
                }
                await db.SaveChangesAsync();
                nuevosMedicamentos++;
            }
            var lote = await db.LoteMedicamentos
                .Where(l => l.IdMedicamento == existente.IdMedicamento && l.FechaVencimiento >= ahora.Date)
                .OrderByDescending(l => l.Stock)
                .FirstOrDefaultAsync();
            medicamentos[m.Codigo] = (existente.IdMedicamento, lote?.IdLote ?? 0);
        }

        // 4. Residentes nuevos con todo lo suyo
        for (int i = 0; i < Residentes.Length; i++)
        {
            var d = Residentes[i];
            var existente = await db.Residentes.FirstOrDefaultAsync(r => r.CodigoUnico == d.Codigo);
            if (existente != null)
            {
                if (existente.Nombre != d.Nombre || existente.Apellido != d.Apellido)
                    avisos.Add($"El código {d.Codigo} ya lo tiene {existente.Nombre} {existente.Apellido}; no se cargó a {d.Nombre} {d.Apellido}.");
                continue; // ya estaba cargado
            }

            var residente = new Residente
            {
                CodigoUnico = d.Codigo,
                Nombre = d.Nombre,
                Apellido = d.Apellido,
                FechaNacimiento = d.Nacimiento,
                NumHabitacion = d.Habitacion,
                TipoSangre = d.Sangre,
                Alergias = d.Alergias,
                ContactoEmergencia = d.Contacto,
                Observaciones = d.Antecedentes,
                Estado = d.DiasBaja == 0
            };
            db.Residentes.Add(residente);
            await db.SaveChangesAsync();
            nuevosResidentes++;

            // Un residente dado de baja tiene su historial hasta el día de la baja
            var activo = d.DiasBaja == 0;
            var referencia = ahora.AddDays(-d.DiasBaja);
            DateTime? baja = activo ? null : referencia.Date.AddHours(16);
            var ingreso = ahora.Date.AddDays(-d.DiasIngreso).AddHours(10);
            var cuidador = d.Cuidador;
            var inicioCuidador = ingreso;
            if (d.CambioDeCuidador)
            {
                // Antes lo cuidaba la cuidadora que ya no trabaja en el hogar
                inicioCuidador = ahora.Date.AddDays(-DiasCambioCuidador).AddHours(8);
                db.AsignacionCuidados.Add(new AsignacionCuidado
                {
                    IdResidente = residente.IdResidente, IdEmpleado = empleados[CuidadoraAnterior],
                    FechaInicio = ingreso, FechaFin = inicioCuidador, Estado = false
                });
            }
            db.AsignacionCuidados.AddRange(
                new AsignacionCuidado { IdResidente = residente.IdResidente, IdEmpleado = empleados[d.Medico], FechaInicio = ingreso, FechaFin = baja, Estado = activo },
                new AsignacionCuidado { IdResidente = residente.IdResidente, IdEmpleado = empleados[cuidador], FechaInicio = inicioCuidador, FechaFin = baja, Estado = activo });

            foreach (var f in Familiares.Where(f => f.Codigo == d.Codigo))
            {
                var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == f.Correo);
                if (usuario != null && usuario.IdRol != roles[RolesSistema.Familiar])
                {
                    avisos.Add($"La cuenta {f.Correo} ya existe con otro rol; no se vinculó como familiar de {d.Nombre} {d.Apellido}.");
                    continue;
                }
                if (usuario == null)
                {
                    usuario = new Usuario { Correo = f.Correo, IdRol = roles[RolesSistema.Familiar], ContrasenaHash = DatosIniciales.ContrasenaPorDefectoHash, Estado = true };
                    db.Usuarios.Add(usuario);
                    await db.SaveChangesAsync();
                    nuevosFamiliares++;
                }
                db.FamiliarResidentes.Add(new FamiliarResidente
                {
                    IdResidente = residente.IdResidente, IdUsuario = usuario.IdUsuario, Parentesco = f.Parentesco, TelefonoContacto = f.Telefono
                });
            }
            await db.SaveChangesAsync();

            var azar = new Random(2026 + i);
            atenciones += await BitacoraAsync(db, residente.IdResidente, d, i, empleados, usuarios[Direccion], tipos, referencia, azar);
            alertas += await AlertasAsync(db, residente.IdResidente, d, i, empleados, usuarios, medicamentos, referencia, activo);
            if (activo) entregas += await EntregasAsync(db, residente.IdResidente, d, empleados[cuidador], medicamentos, ahora);
        }

        if (transaccion != null) await transaccion.CommitAsync();
        return new ResultadoPresentacion(nuevosResidentes, nuevosPersonal, nuevosFamiliares, nuevosMedicamentos, atenciones, alertas, entregas, avisos);
    }

    // ---------------------------------------------------------------- bitácora

    private static readonly string[] Desayunos =
    {
        "frijoles molidos, huevo revuelto, queso fresco y tortilla",
        "avena con plátano y pan francés",
        "pan dulce con café con leche",
        "dos pupusas de queso con curtido",
        "plátano cocido, crema y frijoles",
        "atol de elote y pan con frijoles"
    };

    private static readonly string[] Almuerzos =
    {
        "sopa de pollo con verduras y arroz",
        "pollo guisado con arroz y ensalada",
        "carne asada con frijoles y tortilla",
        "pescado al vapor con verduras",
        "sopa de frijoles con huevo",
        "pasta con carne molida y vegetales",
        "lomo en salsa con puré de papa"
    };

    private static readonly string[] Higienes =
    {
        "Baño en regadera con ayuda; cambio de ropa de cama.",
        "Aseo personal, cepillado de dientes y corte de uñas.",
        "Baño asistido en silla de ducha. Piel hidratada, sin lesiones.",
        "Baño y lavado de cabello. Se aplicó crema humectante en piernas."
    };

    private static readonly string[] Actividades =
    {
        "Caminata de 20 minutos en el jardín con acompañamiento.",
        "Ejercicios de estiramiento en grupo durante 30 minutos.",
        "Terapia física de rodillas y tobillos (15 repeticiones por ejercicio).",
        "Ejercicios de equilibrio con apoyo en barras paralelas."
    };

    private static readonly string[] Recreaciones =
    {
        "Participó en la lotería del salón; estuvo animado.",
        "Escuchó música y conversó con sus compañeros.",
        "Taller de manualidades: pintó un cuadro con acuarelas.",
        "Momento de oración en la capilla.",
        "Vio una película en el salón principal."
    };

    private static async Task<int> BitacoraAsync(
        VitaliaDbContext db, int idResidente, ResidenteDato d, int indice, int[] empleados, int idDireccion,
        Dictionary<string, int> tipos, DateTime ahora, Random azar)
    {
        var cuidador = empleados[d.Cuidador];
        var medico = empleados[d.Medico];
        var lista = new List<AtencionDiaria>();
        var nombresMed = d.Medicamentos
            .Select(c => Medicamentos.First(m => m.Codigo == c))
            .Select(m => $"{m.Nombre} {string.Join(' ', m.Presentacion.Split(' ').Skip(1))}")
            .ToList();

        void Agregar(DateTime cuando, string tipo, int empleado, string texto)
        {
            if (cuando <= ahora)
                lista.Add(new AtencionDiaria
                {
                    IdResidente = idResidente, IdEmpleado = empleado, IdTipoAtencion = tipos[tipo],
                    FechaHora = cuando, Observaciones = texto, EstadoCumplimiento = "Completado"
                });
        }

        for (int dia = Math.Min(DiasBitacora, d.DiasIngreso); dia >= 0; dia--)
        {
            var fecha = ahora.Date.AddDays(-dia);
            int Min() => azar.Next(0, 25);
            var enfermera = empleados[Enfermeria[(dia + indice) % Enfermeria.Length]];

            var come = azar.Next(10) < 8 ? "Desayunó completo: " : "Comió la mitad del desayuno: ";
            var desayuno = Desayunos[azar.Next(Desayunos.Length)];
            if (d.Condicion == "diabetes") desayuno += " (café sin azúcar)";
            Agregar(fecha.AddHours(7).AddMinutes(10 + Min()), Alimentacion, cuidador, come + desayuno + ".");

            Agregar(fecha.AddHours(8).AddMinutes(Min()), Medicacion, cuidador,
                $"Se administró {string.Join(" y ", nombresMed)} según indicación médica. Sin reacciones.");

            Agregar(fecha.AddHours(9).AddMinutes(Min()), SignosVitales, enfermera, Signos(d.Condicion, azar));

            if ((dia + indice) % 2 == 0)
                Agregar(fecha.AddHours(10).AddMinutes(15 + Min()), Higiene, cuidador, Higienes[azar.Next(Higienes.Length)]);

            if (d.Condicion == "acv" && dia % 2 == 1)
                Agregar(fecha.AddHours(11).AddMinutes(Min()), Curacion, enfermera,
                    "Curación de úlcera por presión en talón izquierdo. Evoluciona bien, sin signos de infección.");

            Agregar(fecha.AddHours(12).AddMinutes(20 + Min()), Alimentacion, cuidador,
                (azar.Next(10) < 8 ? "Almorzó completo: " : "Dejó la mitad del almuerzo: ") + Almuerzos[azar.Next(Almuerzos.Length)] + ".");

            if (azar.Next(10) < 6)
            {
                var (tipo, textos) = d.Condicion is "acv" or "demencia" || azar.Next(2) == 0
                    ? (Recreacion, Recreaciones) : (ActividadFisica, Actividades);
                Agregar(fecha.AddHours(15).AddMinutes(Min()), tipo, cuidador, textos[azar.Next(textos.Length)]);
            }

            if ((dia + indice) % 7 == 3)
                Agregar(fecha.AddHours(10).AddMinutes(30 + Min()), ControlMedico, medico, Control(d.Condicion));
        }

        db.AtencionDiaria.AddRange(lista);
        await db.SaveChangesAsync();

        // Algunas atenciones corregidas o anuladas por la dirección, como pasa en el día a día
        if (indice % 5 == 1)
        {
            var medicacion = lista.Where(a => a.IdTipoAtencion == tipos[Medicacion]).Skip(3).FirstOrDefault();
            if (medicacion != null)
                db.CorreccionAtencions.Add(new CorreccionAtencion
                {
                    IdAtencion = medicacion.IdAtencion, IdUsuarioAdmin = idDireccion, TipoAccion = "Corrección",
                    Motivo = "La hora correcta de la administración fue 8:30 a. m.; se anotó tarde en el turno.",
                    FechaHora = medicacion.FechaHora.AddHours(5)
                });
        }
        if (indice % 5 == 3 && lista.Count > 10)
        {
            var original = lista[^10];
            var duplicada = new AtencionDiaria
            {
                IdResidente = idResidente, IdEmpleado = original.IdEmpleado, IdTipoAtencion = original.IdTipoAtencion,
                FechaHora = original.FechaHora.AddMinutes(4), Observaciones = original.Observaciones, EstadoCumplimiento = "Completado"
            };
            db.AtencionDiaria.Add(duplicada);
            await db.SaveChangesAsync();
            db.CorreccionAtencions.Add(new CorreccionAtencion
            {
                IdAtencion = duplicada.IdAtencion, IdUsuarioAdmin = idDireccion, TipoAccion = "Anulación",
                Motivo = "Registro duplicado por error al guardar dos veces.",
                FechaHora = duplicada.FechaHora.AddHours(2)
            });
            lista.Add(duplicada);
        }
        await db.SaveChangesAsync();
        return lista.Count;
    }

    private static string Signos(string condicion, Random azar)
    {
        int sistolica = condicion is "hipertension" or "cardiaco" or "gastritis" ? azar.Next(126, 146) : azar.Next(110, 131);
        int diastolica = azar.Next(70, 89);
        int pulso = azar.Next(64, 86);
        var temperatura = (36.2 + azar.Next(0, 8) / 10.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        int saturacion = condicion == "epoc" ? azar.Next(90, 95) : azar.Next(94, 99);
        var texto = $"Presión arterial {sistolica}/{diastolica} mmHg, pulso {pulso} lpm, temperatura {temperatura} °C, saturación {saturacion} %.";
        if (condicion == "diabetes") texto += $" Glucometría en ayunas: {azar.Next(105, 165)} mg/dL.";
        if (condicion == "cardiaco") texto += $" Peso: {azar.Next(68, 71)}.{azar.Next(0, 10)} kg, sin edema en tobillos.";
        return texto;
    }

    private static string Control(string condicion) => condicion switch
    {
        "hipertension" => "Revisión semanal: presión arterial dentro de metas. Se mantiene el tratamiento.",
        "diabetes" => "Revisión de glucometrías de la semana: valores aceptables. Se refuerza la dieta sin azúcar.",
        "demencia" => "Evaluación cognitiva: estable. Mantener rutina diaria y acompañamiento por las tardes.",
        "epoc" => "Auscultación con sibilancias leves. Continuar con inhalador y oxígeno nocturno.",
        "artrosis" => "Dolor de rodillas controlado. Continuar terapia física tres veces por semana.",
        "cardiaco" => "Sin edema ni falta de aire. Peso estable. Continuar con diurético por la mañana.",
        "tiroides" => "Buen estado general. Pendiente control de hormonas tiroideas el próximo mes.",
        "acv" => "Mejoría leve de fuerza en brazo izquierdo. Continuar movilización y curaciones.",
        "osteoporosis" => "Sin dolor de cadera. Continuar con calcio y vigilar riesgo de caídas.",
        "parkinson" => "Temblor estable con el tratamiento actual. Se recomienda ejercicios de motricidad fina.",
        "gastritis" => "Sin molestias gástricas esta semana. Mantener dieta blanda y omeprazol en ayunas.",
        _ => "Revisión general sin novedades."
    };

    // ---------------------------------------------------------------- alertas

    private record AlertaPlan(string Titulo, string Descripcion, string Prioridad, string Estado, double Horas, int Responsable, string? Medicamento = null);

    private static List<AlertaPlan> PlanAlertas(ResidenteDato d, int indice)
    {
        var lista = new List<AlertaPlan>
        {
            new("Control de peso mensual", "Pesar al residente en ayunas y anotarlo en la bitácora.",
                AlertaPrioridades.Rutina, AlertaEstados.Atendida, -24 * (3 + indice % 4), d.Cuidador),
            indice % 3 == 0
                ? new("Cita de control en el Hospital Rosales", "Llevar expediente y lista de medicamentos. Salida a las 7:00 a. m.",
                    AlertaPrioridades.Programada, AlertaEstados.Pendiente, 24 * (2 + indice % 5), d.Cuidador)
                : indice % 3 == 1
                    ? new("Toma de muestras de laboratorio", "Hemograma, glucosa y perfil de lípidos. Debe estar en ayunas desde las 10:00 p. m.",
                        AlertaPrioridades.Programada, AlertaEstados.Pendiente, 24 * (1 + indice % 4), Enfermeria[indice % 3])
                    : new("Vacuna contra la influenza", "Aplicar la vacuna de la campaña anual y anotar el lote en el expediente.",
                        AlertaPrioridades.Programada, AlertaEstados.EnProceso, 6, Enfermeria[indice % 3]),
            new("Revisión de medicamentos de la semana", "Verificar que el residente tenga sus medicamentos completos para los próximos 7 días.",
                AlertaPrioridades.Rutina, indice % 2 == 0 ? AlertaEstados.Pendiente : AlertaEstados.Atendida, indice % 2 == 0 ? 30 : -48, d.Cuidador)
        };

        switch (d.Condicion)
        {
            case "diabetes":
                lista.Add(new("Glucosa alta en la mañana", "Glucometría de 245 mg/dL antes del desayuno. Repetir en 2 horas y avisar al médico.",
                    AlertaPrioridades.Urgente, indice == 1 ? AlertaEstados.EnProceso : AlertaEstados.Atendida, indice == 1 ? -2 : -70, Enfermeria[0]));
                lista.Add(new("Ajuste de dosis de " + (d.Medicamentos[0] == "MED-111" ? "insulina" : "metformina"),
                    "Aplicar la nueva dosis indicada por el médico a partir de mañana.",
                    AlertaPrioridades.Programada, AlertaEstados.Pendiente, 20, d.Medico, d.Medicamentos[0]));
                break;
            case "epoc":
                lista.Add(new("Saturación de oxígeno baja", "Saturación de 88 % al despertar. Se colocó oxígeno a 2 litros por minuto.",
                    AlertaPrioridades.Urgente, AlertaEstados.Atendida, -30, Enfermeria[1]));
                break;
            case "demencia":
                lista.Add(new("Episodio de desorientación", "Por la tarde no reconocía su habitación. Acompañar y avisar a la familia.",
                    AlertaPrioridades.Urgente, AlertaEstados.Pendiente, -1, d.Cuidador));
                break;
            case "osteoporosis":
                lista.Add(new("Caída leve en el baño", "Resbaló al salir de la ducha, sin lesiones aparentes. Revisar cadera y vigilar 24 horas.",
                    AlertaPrioridades.Urgente, AlertaEstados.Atendida, -52, d.Medico));
                break;
            case "cardiaco":
                lista.Add(new("Aumento de peso de 1.5 kg", "Subió 1.5 kg en dos días. Revisar edema y avisar al médico.",
                    AlertaPrioridades.Urgente, AlertaEstados.EnProceso, -5, Enfermeria[2]));
                break;
            case "hipertension" when indice % 2 == 0:
                lista.Add(new("Ajuste de dosis de " + Medicamentos.First(m => m.Codigo == d.Medicamentos[0]).Nombre.ToLower(),
                    "Presión elevada en las últimas tomas. Aplicar la dosis indicada y tomar la presión cada 8 horas.",
                    AlertaPrioridades.Programada, AlertaEstados.Pendiente, 10, d.Medico, d.Medicamentos[0]));
                break;
            case "acv":
                lista.Add(new("Cambio de posición cada 2 horas", "Prevenir úlceras por presión: cambiar de posición y revisar talones.",
                    AlertaPrioridades.Rutina, AlertaEstados.EnProceso, -3, d.Cuidador));
                break;
        }
        return lista;
    }

    private static async Task<int> AlertasAsync(
        VitaliaDbContext db, int idResidente, ResidenteDato d, int indice, int[] empleados, int[] usuarios,
        Dictionary<string, (int Id, int Lote)> medicamentos, DateTime ahora, bool activo)
    {
        // Si el residente ya se dio de baja, todo lo suyo quedó atendido
        var planes = PlanAlertas(d, indice)
            .Select(p => activo ? p : p with { Estado = AlertaEstados.Atendida, Horas = -Math.Abs(p.Horas) - 24 })
            .ToList();
        foreach (var plan in planes)
        {
            var programada = ahora.AddHours(plan.Horas);
            programada = programada.Date.AddHours(programada.Hour).AddMinutes(programada.Minute < 30 ? 0 : 30);
            var creador = plan.Medicamento != null ? usuarios[d.Medico] : usuarios[Enfermeria[indice % 3]];
            var descripcion = plan.Descripcion;
            // La receta del médico va en la descripción, igual que cuando la crea desde Alertas
            if (plan.Medicamento != null && medicamentos.ContainsKey(plan.Medicamento))
            {
                var m = Medicamentos.First(x => x.Codigo == plan.Medicamento);
                descripcion += $"\n\n[PRESCRIPCIÓN MÉDICA: {m.Nombre} ({m.Presentacion}) - Dosis: {m.Dosis}]";
            }
            var alerta = new Alerta
            {
                IdResidente = idResidente,
                IdUsuarioCreador = creador,
                Titulo = plan.Titulo,
                Descripcion = descripcion,
                FechaProgramada = programada,
                Prioridad = plan.Prioridad,
                EstadoActual = plan.Estado,
                IdEmpleadoResponsable = empleados[plan.Responsable]
            };
            db.Alerta.Add(alerta);
            await db.SaveChangesAsync();

            // Historial de cambios de estado, como si el personal la hubiera ido moviendo
            var quien = usuarios[plan.Responsable];
            var inicio = programada < ahora ? programada : ahora.AddHours(-1);
            if (plan.Estado != AlertaEstados.Pendiente)
                db.HistorialEstadoAlerta.Add(new HistorialEstadoAlerta
                {
                    IdAlerta = alerta.IdAlerta, IdUsuario = quien, EstadoAnterior = AlertaEstados.Pendiente, EstadoNuevo = AlertaEstados.EnProceso,
                    FechaHora = inicio.AddMinutes(10), Comentario = "Se comenzó a atender."
                });
            if (plan.Estado == AlertaEstados.Atendida)
                db.HistorialEstadoAlerta.Add(new HistorialEstadoAlerta
                {
                    IdAlerta = alerta.IdAlerta, IdUsuario = quien, EstadoAnterior = AlertaEstados.EnProceso, EstadoNuevo = AlertaEstados.Atendida,
                    FechaHora = inicio.AddMinutes(55), Comentario = "Atendida sin complicaciones."
                });
        }
        await db.SaveChangesAsync();
        return planes.Count;
    }

    // ---------------------------------------------------------------- entregas de medicamentos

    private static async Task<int> EntregasAsync(
        VitaliaDbContext db, int idResidente, ResidenteDato d, int idCuidador,
        Dictionary<string, (int Id, int Lote)> medicamentos, DateTime ahora)
    {
        int total = 0;
        for (int dia = Math.Min(DiasEntregas, d.DiasIngreso); dia >= 0; dia--)
        {
            var cuando = ahora.Date.AddDays(-dia).AddHours(8).AddMinutes(5);
            if (cuando > ahora) continue;
            foreach (var codigo in d.Medicamentos)
            {
                if (!medicamentos.TryGetValue(codigo, out var med) || med.Lote == 0) continue;
                db.EntregaMedicamentos.Add(new EntregaMedicamento
                {
                    IdLote = med.Lote, IdResidente = idResidente, IdEmpleado = idCuidador, Cantidad = 1, FechaHora = cuando,
                    Observaciones = "Dosis de la mañana"
                });
                total++;
            }
        }
        await db.SaveChangesAsync();
        return total;
    }

    // ================================================================ quitar

    /// <summary>
    /// Quita solo los datos de presentación (residentes, cuentas y medicamentos de este paquete)
    /// y todo lo que depende de ellos. Lo demás de la base no se toca.
    /// </summary>
    public static async Task<ResultadoPresentacion> QuitarAsync(VitaliaDbContext db)
    {
        await using var transaccion = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync() : null;

        var codigos = Residentes.Select(r => r.Codigo).ToList();
        var residentes = (await db.Residentes.Where(r => codigos.Contains(r.CodigoUnico)).ToListAsync())
            .Where(r => Residentes.Any(d => d.Codigo == r.CodigoUnico && d.Nombre == r.Nombre && d.Apellido == r.Apellido))
            .ToList();
        var correos = CorreosDelPaquete();
        var usuarios = await db.Usuarios.Where(u => correos.Contains(u.Correo)).ToListAsync();
        var codigosMed = Medicamentos.Select(m => m.Codigo).ToList();
        var medicamentos = (await db.Medicamentos.Where(m => codigosMed.Contains(m.Codigo)).ToListAsync())
            .Where(m => Medicamentos.Any(d => d.Codigo == m.Codigo && d.Nombre == m.Nombre))
            .ToList();

        var R = residentes.Select(r => r.IdResidente).ToList();
        var U = usuarios.Select(u => u.IdUsuario).ToList();
        var M = medicamentos.Select(m => m.IdMedicamento).ToList();
        var empleados = await db.Empleados.Where(e => U.Contains(e.IdUsuario)).ToListAsync();
        var E = empleados.Select(e => e.IdEmpleado).ToList();
        var lotes = await db.LoteMedicamentos.Where(l => M.Contains(l.IdMedicamento)).ToListAsync();
        var L = lotes.Select(l => l.IdLote).ToList();

        var entregas = await db.EntregaMedicamentos
            .Where(x => R.Contains(x.IdResidente) || L.Contains(x.IdLote) || E.Contains(x.IdEmpleado)).ToListAsync();
        db.EntregaMedicamentos.RemoveRange(entregas);

        var alertas = await db.Alerta
            .Where(a => R.Contains(a.IdResidente) || U.Contains(a.IdUsuarioCreador) ||
                        (a.IdEmpleadoResponsable != null && E.Contains(a.IdEmpleadoResponsable.Value)))
            .ToListAsync();
        var A = alertas.Select(a => a.IdAlerta).ToList();
        db.HistorialEstadoAlerta.RemoveRange(await db.HistorialEstadoAlerta.Where(h => A.Contains(h.IdAlerta) || U.Contains(h.IdUsuario)).ToListAsync());
        db.Alerta.RemoveRange(alertas);

        var atenciones = await db.AtencionDiaria.Where(a => R.Contains(a.IdResidente) || E.Contains(a.IdEmpleado)).ToListAsync();
        var T = atenciones.Select(a => a.IdAtencion).ToList();
        db.CorreccionAtencions.RemoveRange(await db.CorreccionAtencions.Where(c => T.Contains(c.IdAtencion) || U.Contains(c.IdUsuarioAdmin)).ToListAsync());
        db.AtencionDiaria.RemoveRange(atenciones);

        db.FamiliarResidentes.RemoveRange(await db.FamiliarResidentes.Where(f => R.Contains(f.IdResidente) || U.Contains(f.IdUsuario)).ToListAsync());
        db.AsignacionCuidados.RemoveRange(await db.AsignacionCuidados.Where(a => R.Contains(a.IdResidente) || E.Contains(a.IdEmpleado)).ToListAsync());
        await db.SaveChangesAsync();

        db.LoteMedicamentos.RemoveRange(lotes);
        db.Medicamentos.RemoveRange(medicamentos);
        db.Empleados.RemoveRange(empleados);
        await db.SaveChangesAsync();
        db.Usuarios.RemoveRange(usuarios);
        db.Residentes.RemoveRange(residentes);
        await db.SaveChangesAsync();

        if (transaccion != null) await transaccion.CommitAsync();
        var personal = usuarios.Count(u => Personal.Any(p => p.Correo == u.Correo));
        return new ResultadoPresentacion(residentes.Count, personal, usuarios.Count - personal, medicamentos.Count,
            atenciones.Count, alertas.Count, entregas.Count, new List<string>());
    }
}
