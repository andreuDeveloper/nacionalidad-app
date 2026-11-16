# Nacionalidad España — CCSE + DELE A2

App Android (.NET MAUI) para preparar los dos exámenes de la nacionalidad española por residencia.
Ver `CLAUDE.md` para las decisiones de producto.

## Estado

| Fase | Contenido | Estado |
|---|---|---|
| 1 | Motor de tests + CCSE completo en español | ✅ Hecho |
| 2 | Traducción de explicaciones (ar, en, fr, ur) + RTL | ✅ Hecho — **árabe y urdu pendientes de revisión por un nativo** |
| 3 | DELE A2: lectora y auditiva | ⏳ |
| 4 | DELE A2: escrita y oral | ⏳ |
| 5 | Pagos Google Play y paywall | 🟡 `BillingService` hecho; paywall desactivado (`FeatureFlags.DeleAvailable`) |
| 6 | Ficha de la tienda (ASO) | ⏳ |

## Estructura

```
docs/CCSE_2026.pdf                 Manual oficial del Instituto Cervantes (fuente de las preguntas)
src/ExamEngine.Core/               Motor genérico (sin MAUI ni nada de CCSE/DELE)
  Models/                          QuestionBank, Question, ExamBlueprint, LocalizedText
  Loading/                         Carga y validación del JSON (schemaVersion)
  Sessions/                        SessionFactory (práctica, simulacro, repaso), ExamSession, ExamResult
  Progress/                        IProgressStore (persistencia), ProgressTracker (repaso de fallos)
  Statistics/                      StatisticsCalculator
src/Nacionalidad.App/              App .NET MAUI
  Content/ccse.json                Banco CCSE generado (300 preguntas + explicaciones)
  Data/                            SQLite (sqlite-net-pcl): implementación de IProgressStore
  Services/                        Idioma/RTL, carga de contenido, Google Play Billing
  Localization/                    Translator + {loc:Tr Clave} para XAML (cambio de idioma en caliente)
  Resources/Strings/               AppResources.resx (es) + .ar .en .fr .ur
  ViewModels/  Views/              MVVM con CommunityToolkit.Mvvm
tests/ExamEngine.Core.Tests/       xUnit (también valida los JSON de Content/)
tools/ccse/                        Generador del banco CCSE desde el PDF + explicaciones en .tsv
tools/i18n/check_resx.py           Comprueba que todos los idiomas tienen las mismas claves
```

## Compilar y probar

Requisitos: .NET 10 SDK, workload `maui-android`, Android SDK (Visual Studio 2026 o Rider lo instalan).

```bash
dotnet workload install maui-android
```

```bash
dotnet test tests/ExamEngine.Core.Tests
```

```bash
dotnet build src/Nacionalidad.App -f net10.0-android -t:Run
```

En Visual Studio: abrir `NacionalidadApp.sln`, proyecto de inicio `Nacionalidad.App`, elegir un emulador o
móvil Android y F5.

## Funcionamiento del CCSE

- **Simulacro**: 25 preguntas (10 de la tarea 1, 3 de la 2, 2 de la 3, 5 de la 4 y 5 de la 5), 45 minutos,
  aprobado con 15. Se pueden cambiar las respuestas hasta entregar; si se acaba el tiempo se entrega solo.
- **Práctica por tareas**: corrección inmediata con explicación; primero salen las preguntas nunca vistas.
- **Repaso de fallos**: una pregunta fallada se queda en el repaso hasta acertarla 2 veces seguidas.
- **Estadísticas**: vistas, dominadas (3 aciertos seguidos), % de aciertos, simulacros aprobados, racha de días
  y una "preparación estimada".
- Las preguntas se muestran siempre en español (como en el examen). La interfaz y las explicaciones, en el
  idioma elegido; si una explicación no está traducida, se muestra en español con un aviso.

## Actualizar el banco del CCSE (cada año)

El Cervantes renueva cada año parte de las preguntas. Con el manual nuevo:

```bash
pip install pymupdf
```

```bash
python3 tools/ccse/build_ccse_bank.py docs/CCSE_2027.pdf src/Nacionalidad.App/Content/ccse.json --content-version 2027.1 --year 2027
```

El script lee preguntas y soluciones del PDF, añade las explicaciones de `tools/ccse/explicaciones_<idioma>.tsv`
(`id<TAB>texto`), informa de las preguntas nuevas/eliminadas/cambiadas y avisa de las que no tienen explicación.
Después, `dotnet test` valida el JSON. El progreso del usuario se guarda por id de pregunta (`ccse-1001`…), así
que las preguntas que desaparecen dejan de contar en las estadísticas automáticamente.

## Traducir explicaciones

Están en `tools/ccse/explicaciones_<idioma>.tsv` (es, en, fr, ar, ur), mismo formato que `explicaciones_es.tsv`
(las líneas que empiezan por `#` son comentarios). Tras editarlas, volver a ejecutar el script.
Las de árabe y urdu las tiene que revisar un nativo antes del lanzamiento.

## Pagos (fase 5)

- Producto integrado no consumible en Google Play Console con id `pase_dele_a2` (14,99 €).
- `BillingService` compra, reconoce (acknowledge) y restaura; el derecho se cachea en `Preferences` para
  funcionar sin conexión.
- El paywall no se muestra hasta poner `FeatureFlags.DeleAvailable = true`.
