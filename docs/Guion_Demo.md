# VITALIA: guion de la demo para la presentación final

**Duración:** unos 15 minutos de demo, más preguntas.
**Idea central:** contar **un día en el hogar** siguiendo a un residente, **José Ernesto Guardado** (RES-2026-102, diabetes e hipertensión), y pasando por los 4 roles: la directora, la médica, la enfermera y el hijo.

---

## 1. Antes de presentar (el día anterior y 30 minutos antes)

### 1.1 Preparar la base

1. Encender VITALIA con una base que **no** tenga los datos de presentación.
2. Entrar como `admin@vitalia.com` y en **Sistema → Datos de presentación** presionar **Cargar datos de presentación**.
3. Revisar que el resumen diga **16 residentes, 11 del personal, 10 familiares, 16 medicamentos**, unas 1235 atenciones, 60 alertas y 200 entregas, **sin avisos**.

Las fechas de la bitácora y de las alertas se calculan desde el momento en que se cargan. **Cárguenlas el día anterior o la misma mañana**, para que "hoy" tenga atenciones y las alertas próximas sigan en el futuro.

### 1.2 Revisar el equipo

- [ ] Servidor encendido y página de inicio de sesión abierta.
- [ ] `wkhtmltopdf.exe` en `wwwroot/Rotativa/` (sin esto no salen los PDF).
- [ ] Navegador con zoom al 110 % o 125 % para que se lea en el proyector.
- [ ] Una ventana normal y **una ventana de incógnito** (para tener dos sesiones a la vez: personal y familiar).
- [ ] Hacer un **respaldo** después de cargar los datos, por si hay que volver atrás.
- [ ] Tener a mano la contraseña por defecto de las cuentas (la tiene Josué; **no se escribe en las diapositivas**).

### 1.3 Cuentas que se usan

Todas las cuentas de los datos de presentación tienen la **contraseña por defecto**.

| Rol | Cuenta | Quién es | Para qué se usa en la demo |
|---|---|---|---|
| Administrador | `claudia.arevalo@vitalia.com` | Claudia Arévalo, directora administrativa | Panel, residentes, personal, asignaciones, vínculos, usuarios, respaldos |
| Médico | `patricia.villalta@vitalia.com` | Patricia Villalta, médica geriatra (atiende a José Ernesto) | Ficha clínica, alerta con receta, farmacia |
| Enfermero | `sandra.alvarado@vitalia.com` | Sandra Alvarado, enfermera jefe | Bitácora y mover alertas |
| Enfermero (cuidador) | `rosa.escobar@vitalia.com` | Rosa Escobar, cuidadora de José Ernesto | Cuenta de respaldo si falla la de Sandra |
| Cuenta desactivada | `alicia.benitez@vitalia.com` | Alicia Benítez, ya no trabaja en el hogar | Mostrar que no puede entrar |
| Familiar | `ernesto.guardado@hotmail.com` | Ernesto Guardado, hijo de José Ernesto | Vista del familiar y PDF |
| Familiar (respaldo) | `lucia.rodriguez.z@gmail.com` | Lucía Rodríguez, hija de Ana Cecilia (RES-2026-103, Alzheimer) | Otra vista del familiar, con una alerta urgente pendiente |

### 1.4 Quién presenta qué (sugerencia)

| Parte | Tema | Dueño sugerido |
|---|---|---|
| 0 y 1 | Inicio de sesión y roles | Josué |
| 2 | Administrador: residentes, personal y asignaciones | Josué y David |
| 3 | Médico: alerta con receta y farmacia | Jenni y Ludy |
| 4 | Enfermería: bitácora y PDF | Yeyson |
| 5 | Familiar | Josué |
| 6 | Respaldos y cierre | Ludy |

Ajústenlo a lo que hizo cada quien; lo importante es que cada uno muestre su módulo.

---

## 2. La demo, paso a paso

### Parte 0. Presentación (1 minuto)

Decir en una frase qué es VITALIA: *"un sistema para el hogar de adultos mayores que junta el expediente, la bitácora diaria, las alertas, la farmacia y la información para la familia, con permisos por rol."*

Mencionar la tecnología: ASP.NET Core MVC (.NET 10), SQL Server con Entity Framework, inicio de sesión con roles y contraseñas encriptadas con BCrypt, y PDF con Rotativa.

### Parte 1. Inicio de sesión y seguridad (1 minuto)

1. En la pantalla de inicio de sesión, escribir `alicia.benitez@vitalia.com` con la contraseña por defecto.
   - **Se ve:** "Este usuario está desactivado. Consulte con el administrador."
   - **Se dice:** "Alicia ya no trabaja aquí. Su cuenta existe para conservar su historial, pero ya no puede entrar."
2. Escribir una contraseña equivocada con cualquier cuenta.
   - **Se ve:** "Correo o contraseña incorrectos." El sistema no dice cuál de los dos falló.

### Parte 2. Administrador (4 minutos) · `claudia.arevalo@vitalia.com`

1. **Entrar.** Mostrar el **Panel principal** con los totales y el menú completo, incluida la sección **Sistema**.
2. **Tarjeta de perfil.** Tocar la tarjeta de abajo a la izquierda: nombre, correo, rol, cargo y los botones de contraseña y cerrar sesión. Cerrarla con Esc.
3. **Residentes.**
   - Buscar `Guardado` y seleccionar a **José Ernesto Guardado Méndez**: habitación 102, sangre A+, diabetes tipo 2.
   - Cambiar el filtro a **Inactivos**: aparece **Ernesto Palacios** (RES-2026-116). "Se trasladó a Santa Ana. No se borra: su expediente queda guardado."
   - Volver a **Activos** y abrir la **ficha** de José Ernesto. Mostrar su médica (Patricia Villalta), su cuidadora (Rosa Escobar) y sus **dos familiares** (hijo y nieta).
4. **Validación rápida (opcional).** En **Registrar residente**, poner una fecha de nacimiento de alguien de 40 años y guardar: "El residente debe tener al menos 60 años." Cancelar.
5. **Personal y asignaciones.**
   - Abrir **Personal**: 2 médicos, 3 de enfermería, 4 cuidadores y la directora.
   - Filtrar **Inactivos**: aparece Alicia Benítez.
   - Mostrar que cada residente tiene **un médico y un cuidador**. Presionar **Nueva** e intentar asignar a otro cuidador a José Ernesto: el sistema lo impide y explica por qué.
6. **Usuarios y Roles.** Mostrar la lista: personal con `@vitalia.com` y familiares con sus correos personales; Alicia aparece inactiva. Mostrar la pestaña **Roles** con los 4 roles.

### Parte 3. Médica (3 minutos) · `patricia.villalta@vitalia.com`

Usar la ventana normal: cerrar sesión de Claudia y entrar como Patricia.

1. **Menú.** Señalar que **ya no aparecen** Personal, Usuarios ni Respaldos.
2. **Alertas y Tareas.** Mostrar el tablero con sus 3 columnas. Filtrar por **José Ernesto**: se ve **"Glucosa alta en la mañana"** (Urgente, En proceso) y **"Ajuste de dosis de metformina"** con su receta.
3. **Crear una alerta con receta.**
   - **Nueva alerta** → residente **José Ernesto Guardado**, responsable **Sandra Alvarado**.
   - Título: `Control de glucosa antes de la cena`. Prioridad: **Programada**. Fecha: hoy a las 5:00 p. m.
   - Como es médica, le aparece la lista de medicamentos: elegir **Metformina** y dosis `850 mg con la cena`.
   - **Guardar Alerta.** Abrirla con **Ver**: la receta quedó como `[PRESCRIPCIÓN MÉDICA: Metformina (Tableta 850 mg) - Dosis: 850 mg con la cena]`.
4. **Farmacia y Stock.** Mostrar el catálogo y los estados de las existencias:
   - **Insulina NPH**, **Salbutamol** y **Donepezilo** con stock **Bajo**;
   - **Ibuprofeno** **Agotado**;
   - al abrir el detalle de **Atorvastatina**, se ve que uno de sus dos lotes ya está vencido;
   - **Ranitidina** inactiva (descontinuada, se cambió por omeprazol).

### Parte 4. Enfermería (3 minutos) · `sandra.alvarado@vitalia.com`

1. **Mover las alertas.** En **Alertas y Tareas**, en la alerta nueva de Patricia, presionar **Iniciar → En Proceso**. En "Glucosa alta en la mañana", presionar **Marcar Atendida**. Abrir **Ver** para mostrar el **historial** de cambios.
   - **Se dice:** "Una alerta atendida ya no se puede reabrir; así queda la trazabilidad."
2. **Bitácora.**
   - Filtrar por **José Ernesto** y por el tipo **Signos vitales**: se ven dos semanas de registros.
   - Registrar una atención nueva: residente José Ernesto, tipo **Signos vitales**, empleado **Sandra Alvarado**, observación `Glucometría antes de la cena: 132 mg/dL. Presión 128/80.` → **Guardar atención**.
3. **PDF de la bitácora.** Con el filtro de José Ernesto puesto, presionar el botón de **PDF** y abrir el archivo.

### Parte 5. Familiar (2 minutos) · `ernesto.guardado@hotmail.com`

Usar la **ventana de incógnito**, así la sesión del personal sigue abierta en la otra.

1. **Entrar.** El hijo llega directo a **Mi familiar**. El menú solo tiene esa opción.
2. **Tarjeta del residente.** Muestra a su papá, "Usted es su hijo", la médica y la cuidadora, las alertas activas y la última atención.
3. **Ficha (UC-10 y UC-14).**
   - Aparece la atención que Sandra acaba de registrar: **"Glucometría antes de la cena: 132 mg/dL..."**.
   - En las alertas, "Glucosa alta en la mañana" ya está **atendida** y "Control de glucosa antes de la cena" está activa.
   - Mostrar los **medicamentos que recibe** (metformina y losartán, con quién los entregó).
4. **PDF (UC-11).** Presionar **Descargar historial en PDF** y abrirlo.
5. **Seguridad.** En la barra de direcciones, cambiar el número del residente por otro (por ejemplo, el de al lado): sale **Acceso denegado**. Luego intentar abrir `/Residentes`: también **Acceso denegado**.
   - **Se dice:** "El familiar solo ve a su residente y no puede cambiar nada."

### Parte 6. Respaldos y cierre (1 minuto) · `claudia.arevalo@vitalia.com`

1. Volver a la ventana normal y entrar como Claudia.
2. **Respaldos → Nuevo Respaldo → Generar Respaldo.** Aparece en la lista con su fecha y tamaño, y se puede **Descargar**.
3. **Cierre:** repasar en una frase lo que hace cada rol y agradecer.

---

## 3. Plan B (si algo falla en vivo)

| Si pasa esto | Hacer esto |
|---|---|
| El PDF no se genera | Falta `wkhtmltopdf.exe`. Decir que se mostrará el PDF que ya se generó antes (tener uno descargado en el escritorio). |
| Una cuenta no entra | Usar la de respaldo de la tabla 1.3 (`rosa.escobar@vitalia.com` o `lucia.rodriguez.z@gmail.com`). |
| Se cambió algo por error | Restaurar el respaldo que se hizo en 1.2. |
| La base no tiene los datos | Cargar los datos de presentación en vivo (tarda unos segundos) y seguir. |

## 4. Cosas que conviene NO tocar en vivo

Mientras no se arreglen, estas pantallas tienen detalles que se pueden notar (ver la revisión de errores):

- **No eliminar atenciones** de la bitácora que tengan correcciones o anulaciones: el borrado da error.
- **No editar** medicamentos con **dos lotes** (Losartán, Atorvastatina, Paracetamol): la edición cambia un lote al azar.
- En el **Panel principal**, no señalar el número de **"Alertas activas"** (cuenta también las atendidas) ni el de **medicamentos** (sale en 0).
- En el detalle de una alerta, no señalar el **"Creada por"**: hoy siempre muestra la misma cuenta.

## 5. Preguntas que pueden hacer y cómo responderlas

| Pregunta | Respuesta corta |
|---|---|
| ¿Cómo se guardan las contraseñas? | Encriptadas con BCrypt; ni el administrador puede verlas. |
| ¿Qué pasa si se borra un residente? | No se borra: se da de baja y queda inactivo con todo su historial. |
| ¿El familiar puede ver a otros residentes? | No. El sistema revisa el vínculo en cada pantalla y en el PDF. |
| ¿Cómo se prueban? | Con pruebas automáticas (xUnit) sobre una base en memoria: inicio de sesión, permisos por rol, residentes, familiares, asignaciones, farmacia y datos de presentación. |
| ¿Por qué todos tienen los mismos datos? | Por la opción "Datos de presentación": carga el mismo paquete, una sola vez y sin tocar lo que ya existe. |
