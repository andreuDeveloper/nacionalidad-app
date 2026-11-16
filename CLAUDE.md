# App Nacionalidad España: CCSE + DELE A2

## Qué es
App Android (Google Play) para preparar los dos exámenes de la nacionalidad española por residencia:
- **CCSE** (Conocimientos Constitucionales y Socioculturales de España): **gratis**, sirve como gancho para conseguir instalaciones.
- **DELE A2** (Instituto Cervantes): **de pago**, es el producto principal.

Cliente de pago: residentes en España que no hablan español como lengua nativa (los iberoamericanos están exentos del DELE). Por eso la app es multilingüe.

## Decisiones cerradas (no cambiar sin preguntar)
- **Coste operativo cero.** Sin backend, sin servidor y sin APIs de IA de pago. Todo funciona en el dispositivo y sin conexión.
- **Stack:** .NET MAUI (.NET 10 LTS), C#, MVVM con CommunityToolkit.Mvvm. Solo Android por ahora; no romper la posibilidad de sacarla para iOS en el futuro.
- **Monetización:** pago único de **14,99 €** ("Pase DELE A2"), con Google Play Billing (Plugin.InAppBilling). Sin suscripción. Sin anuncios.
- **Idiomas de la interfaz y de las explicaciones:** español, árabe, inglés, francés y urdu. Árabe y urdu se escriben de derecha a izquierda (RTL).
- **Contenido 100 % original.** No copiar exámenes oficiales del Cervantes. Las preguntas del CCSE salen del manual oficial publicado (está en `/docs`).

## Funcionalidades
### CCSE (gratis)
- Banco de preguntas del Manual CCSE 2026, organizado por tareas y temas.
- Explicación de cada respuesta en los 5 idiomas.
- Modo práctica por tema, simulacro con el formato real (25 preguntas, 45 min, aprobado con 15) y repaso de fallos.
- Estadísticas de progreso.

### DELE A2 (de pago)
- **Comprensión lectora:** tests con corrección automática.
- **Comprensión auditiva:** el audio se genera con TextToSpeech de MAUI (voz es-ES) con velocidad regulable. Corrección automática.
- **Expresión escrita:** el usuario escribe su texto y después ve una respuesta modelo y una lista de criterios para autoevaluarse. La app comprueba sola el número de palabras y algunas palabras clave.
- **Expresión oral:** el usuario graba su respuesta y se transcribe con SpeechToText (CommunityToolkit.Maui, reconocimiento del dispositivo). Luego ve su transcripción junto a la respuesta modelo y puede escuchar su grabación.
- 5 modelos de examen completos.
- Plan de estudio a partir de la fecha de examen.

## Repositorio y estructura
Repo: **`nacionalidad-app`**

```
nacionalidad-app/
├── CLAUDE.md
├── docs/                      # Manual CCSE 2026 (PDF) y material de referencia
├── src/
│   ├── ExamEngine.Core/       # Biblioteca .NET genérica y reutilizable
│   └── Nacionalidad.App/      # App .NET MAUI (CCSE + DELE A2)
├── tests/
│   └── ExamEngine.Core.Tests/ # Tests unitarios del motor (xUnit); también valida los JSON de Content/
└── tools/
    ├── ccse/                  # build_ccse_bank.py (PDF -> Content/ccse.json) + explicaciones_<idioma>.tsv
    └── i18n/                  # check_resx.py: mismas claves y marcadores en los 5 .resx
```

### Convenciones
- El banco del CCSE (`Content/ccse.json`) **se genera** con `tools/ccse/build_ccse_bank.py`; no editarlo a mano.
  Las explicaciones se escriben en `tools/ccse/explicaciones_<idioma>.tsv`.
- Ids de pregunta estables (`ccse-1001`): el progreso del usuario se guarda por id.
- Textos de la interfaz: siempre en los 5 `.resx` (`Resources/Strings`); en XAML con `{loc:Tr Clave}`,
  en C# con `Translator.Instance["Clave"]`. Comprobar con `tools/i18n/check_resx.py`.
- Las preguntas se muestran siempre en español y `FlowDirection="LeftToRight"`, también con interfaz RTL.

### ExamEngine.Core (biblioteca de clases .NET, sin dependencias de MAUI)
- Motor de tests genérico: modelos de pregunta/examen, simulacros, puntuación, repaso de fallos y estadísticas.
- **Prohibido meter aquí nada específico de CCSE o DELE.** Se va a reutilizar en otras apps de examen (`Nautica.App`, `Cap.App`, oposiciones…).
- Carga de bancos de preguntas en JSON versionado (`schemaVersion`).
- Interfaces de persistencia; la implementación con SQLite va en la app.

### Nacionalidad.App (.NET MAUI)
- `Content/`: bancos de preguntas en JSON y textos traducidos. Nada de contenido escrito a mano dentro del código.
- `Data/`: SQLite (sqlite-net-pcl) para progreso, fallos y estadísticas.
- `Services/`: facturación, audio, voz y preferencias.
- `Views/` y `ViewModels/`: MVVM con CommunityToolkit.Mvvm.
- Localización con archivos `.resx` y soporte RTL (`FlowDirection`).

## Plan por fases
1. Motor de tests y CCSE completo en español.
2. Traducción de las explicaciones a los 4 idiomas y soporte RTL.
3. DELE A2: lectora y auditiva.
4. DELE A2: escrita y oral.
5. Pagos con Google Play, paywall y desbloqueo.
6. Ficha de la tienda (ASO): "CCSE 2026", "examen nacionalidad española", "DELE A2", en 5 idiomas.

## Notas
- Si la cuenta de Google Play es personal, hay que hacer un test cerrado con 12 testers durante 14 días antes de publicar.
- El Cervantes renueva cada año un 25 % de las preguntas del CCSE: el banco tiene que ser fácil de actualizar.
- Las traducciones al árabe y al urdu las tiene que revisar un nativo antes del lanzamiento.
