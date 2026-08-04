# Plan de adecuación a VIROO 3.0 — 4 usuarios con experiencia individual

Simulador de Pasteurización VR — UNAD / PIZCBC292025
Basado en el análisis del código, el guion V.2, la ficha del proyecto, la documentación oficial
de VIROO Studio 3.0 y la inspección de los paquetes 3.0.606 instalados.

**Principio rector:** los ajustes son **aditivos**. No se reestructura lo desarrollado.

---

## 0. Diagnóstico real (corregido)

Un primer diagnóstico buscó componentes por nombre dentro del `.unity`, lo cual da **falsos
negativos**: Unity referencia los scripts por GUID, no por nombre. Repetida la verificación por
GUID sobre `Assets/Scenes/Escena 1.unity`, el estado real es **bastante mejor** de lo reportado:

| Requisito VIROO 3.0 | Estado real verificado |
|---|---|
| `Root` con `DependencyInjectionContext` + `DependencyInjectionContextAutoWire` | **Correcto**: ambos presentes en el GameObject `Root` |
| Sin cámaras activas | **Correcto**: la cámara existe pero con el componente deshabilitado |
| Sin `EventSystem` | **Correcto** |
| Interactuables alcanzables por el rayo | **Correcto**: 8 `XRSimpleInteractable`, entre ellos `Maniqui`, `Lavamanos` y `locker room (3)` |
| Canvas VR con `TrackedDeviceGraphicRaycaster` | **Correcto**: 7 canvas, incluido `_SimDashboard` |
| Componentes de red (`NetworkObject`, `VirooXRSimpleInteractable`) | **Ninguno** — y así debe ser (ver §1) |
| Application Identifier en Application Builder | **Vacío** — este sí es bloqueante |

Es decir: la escena **ya está sustancialmente conforme**. El trabajo pendiente es mucho menor
de lo estimado inicialmente.

---

## 1. Modelo de experiencia elegido: individual, con co-presencia

Decisión del equipo: **cada estudiante opera el simulador por su cuenta**, para que los cuatro
vivan la experiencia completa. Lo multiusuario es la **co-presencia**: comparten el espacio
virtual, se ven como avatares y se escuchan, pero cada uno avanza a su ritmo.

Esto tiene una consecuencia técnica que conviene entender bien, porque **simplifica el proyecto
en lugar de complicarlo**:

- Un `XRSimpleInteractable` puro de XRI **ya funciona** en VIROO. El rig del jugador
  (`XR Origin (XR).prefab` del paquete `com.viroo`) trae `NearFarInteractor` en VR y
  `XRMouseInteractor` en escritorio, ambos en interaction layer `Default`. Detectan cualquier
  interactuable estándar sin necesidad de componentes VIROO.
- `VirooXRSimpleInteractable` **no es el interactuable**: es un acompañante cuya única función
  es **retransmitir** el evento a los demás clientes. Verificado en el IL de `Viroo.dll`:
  `OnLocalEventTriggered` sale inmediatamente si `sendNetworkEvents` es falso, y lo local
  siempre lo dispara el `UnityEvent` de XRI, no el componente de VIROO.
- Por tanto, para experiencia individual **no se debe añadir** `NetworkObject` ni
  `VirooXRSimpleInteractable`. Añadirlos haría exactamente lo contrario de lo deseado:
  propagaría el clic de un estudiante a los otros tres.

**Conjunto mínimo correcto por objeto interactuable:**

1. `Collider` **no-trigger** (`XRBaseInteractable.Awake` descarta los triggers).
2. `XRSimpleInteractable` con `interactionLayers` = Default (bit 0; el bit 31 es del teleport).
3. La lógica cableada en `Select Entered` / `Activated` apuntando a los scripts que ya existen.

Si en algún momento se quiere una acción **sí compartida** (por ejemplo un aviso para todos),
la vía es `UnityEventAction.Execute()`; para lo local, `UnityEventNonBroadcastAction`.

### Decisiones confirmadas por el equipo

1. **Panel personal por estudiante.** Cada uno tiene su propio panel flotante del simulador, en
   lugar de un televisor compartido. Así nunca ve valores que no correspondan a su propia
   operación. El `_SimDashboard` actual pasa a instanciarse por jugador y a seguir al usuario
   local (patrón de canvas World Space que ya implementa `PasteurizerWorldCanvas`).
2. **Avance individual.** Cada estudiante pasa al momento siguiente en cuanto termina lo suyo,
   sin esperar a los demás. Se descarta la espera de grupo del guion. **Consecuencia: no se
   requiere ninguna sincronización de red en el flujo**; los fades locales ya implementados
   sirven tal cual.
3. **El explorador de 860 piezas debe funcionar en el visor.** Es el trabajo técnico principal
   que queda (ver §4.3).

---

## 2. Otras decisiones de diseño ya tomadas

- **Una sola escena Unity.** Los tres momentos se resuelven con fade out / fade in dentro de
  `Escena 1`. No se parte la escena y no se usa `LoadSceneAction`.
- **El simulador SCADA se conserva** tal como está.
- El guion y la ficha son referencia, no contrato literal.

---

## 3. Corrección a recomendaciones previas

- ~~Pasar a Single Pass Instanced~~ → **VIROO 3.0 exige OpenXR en modo Multi-Pass.** El proyecto
  ya está así. No tocar.
- ~~El Root no tiene los componentes de inyección de dependencias~~ → **sí los tiene.**
- ~~No hay ningún interactuable ni raycaster de UI~~ → **sí los hay** (8 y 7 respectivamente).
- Sigue vigente: bajar MSAA 8x → 4x y la limpieza de assets.

---

## 4. Trabajo realmente pendiente

### 4.1 Bloqueante para publicar

**Application Builder** (`Window → Viroo → Dashboard`): definir Application Identifier y
Application Name, registrar `Escena 1` y marcarla como escena lobby. No añadir escenas a mano
al Build Settings salvo `VIROO Main` en el índice 0. El build genera un `.zip` que se sube a
VIROO Cloud; no se ejecuta directamente.

### 4.2 Verificaciones sobre lo que ya existe

Correr `Viroo/Adecuacion VIROO/3. Diagnosticar conformidad VIROO`, que comprueba:
colliders no-trigger en los 8 interactuables, interaction layer Default, y —muy importante—
**interactuables sin nada cableado en `Select Entered`**, que es el fallo silencioso más típico
(se pueden apuntar pero no hacen nada).

### 4.3 El explorador de 860 piezas — RESUELTO

**`MultiMeshRayInteraction` se descartó.** Se decompiló `Viroo.Interactions.dll`: no es un
sistema de interacción sino un **asistente de editor**. Su cuerpo en runtime es solo `Awake` e
`Inject`; el botón "Set Interaction" añade **un único** interactable en la raíz y hornea 860
`MeshCollider` en el asset. Consecuencia decisiva: `ActivateEventArgs.interactableObject`
siempre devuelve la raíz, **nunca dice qué pieza se tocó**, que es justo lo que el explorador
necesita. Además arrastra `NetworkObject` obligatorio, contrario a la experiencia individual.
No hay ni un solo prefab, escena o sample que lo use en todo el proyecto ni en los paquetes.

**Solución aplicada:** conservar el raycast propio, que ya produce el `PartHitInfo` con tag ISA
y descripción, y alimentarlo con el interactor del rig de VIROO. En `PasteurizerHoverHandler`:

- `TryBindHandInteractor()` localiza el `NearFarInteractor` de la mano preferida y adopta su
  `curveOrigin` como `raySource`, de modo que el ray coincide exactamente con la línea que el
  estudiante ve. El rig aparece después del `Awake` de la escena, así que se reintenta cada
  0,5 s hasta encontrarlo (no cada frame).
- `IsClickPressed()` consulta `selectInput.ReadWasPerformedThisFrame()` del interactor. Es
  entrada de XRI, no de red: **el pin queda local para cada estudiante**, como se decidió.
- `NotifyExternalClick()` permite además disparar el pin desde un
  `ControllerButtonPressInteraction` de VIROO cableado en el inspector, sin tocar código.
- El fallback de ratón se desactiva solo cuando hay interactor de mano (en escritorio los
  controladores están inactivos y no se encuentran, así que el ratón sigue funcionando).

Todo son tipos de Unity XRI, no de VIROO: no se añade ninguna dependencia de inyección ni de
red al script.

**Limitación conocida de este enfoque:** el gatillo se lee directamente del interactor, así que
el pin también se dispara si el rayo está sobre un panel de UI. Si eso molesta en pruebas, la
evolución natural es un único `XRSimpleInteractable` en la raíz del modelo que resuelva la
subpieza desde el interactor, conservando el mismo `PartHitInfo`.

**Nota sobre `PasteurizerPartsRegistry.cs:53`:** `ByName` descarta piezas con nombre repetido.
No afecta al hover ni al pin (que operan sobre el GameObject realmente impactado), pero sí al
"centrar" del panel lateral, que puede enfocar otra pieza homónima.

### 4.4 Panel personal por estudiante — RESUELTO

El dashboard estaba fijo como hijo del televisor `Simulador`. Ahora se convierte en panel
personal con `Viroo/Adecuacion VIROO/4. Convertir Dashboard en panel personal`.

**Por qué esto basta para que sea personal:** el dashboard no lleva componentes de red, así que
cada cliente tiene su propia instancia del objeto de escena y su propio motor de simulación. Al
hacer que el panel siga a la cámara local, en cada visor se coloca frente a su dueño con sus
propios valores. No hace falta instanciar nada por jugador ni sincronizar nada.

Se añadió a `PasteurizerWorldCanvas` un modo **`LazyFollow`**: el panel se queda quieto mientras
el estudiante lo opera y solo se recoloca cuando se gira más de 22° o se aleja más de 0,6 m.
Es lo correcto para un panel con botones —apuntar a un blanco en movimiento es incómodo— y
además responde al requisito de confort de la ficha, a diferencia del modo `HUDAnchor` rígido.
Método público `Recenter()` por si se quiere un botón "recentrar".

El televisor de la planta queda libre; puede dejarse apagado o con una imagen fija de
ambientación. La operación es reversible con el menú `4b. Devolver Dashboard al televisor`.

### 4.5 Fallos reportados en pruebas y corregidos

| Síntoma | Causa raíz | Corrección |
|---|---|---|
| Pulsar ANTERIOR/SIGUIENTE del carrusel abría la tarjeta del pasteurizador | La UI de Unity no tiene colliders: el raycast físico atravesaba el panel y golpeaba la máquina detrás | Se bloquea el pin mientras el rayo está sobre UI (`uiHoverEntered`/`uiHoverExited` en VR, `IsPointerOverGameObject` en escritorio) |
| Cualquier pieza mostraba siempre la descripción del SKID | **No era el mapeo de nombres** (verificado: 860/860 resuelven bien). `Physics.Raycast` devolvía solo el impacto más cercano y se rendía si no era pieza válida; los muros de `_CollisionWalls` están mal orientados y atraviesan la máquina, dejando alcanzables solo las 18 piezas `Skid_*`/`DripPan_*` | `FirstValidPartAlong()` con `RaycastNonAlloc`: atraviesa colliders ajenos y devuelve la pieza válida más cercana |
| La tarjeta congelaba el pin | Flota a 0,85 m ocupando 1,41 × 0,70 m y sus gráficos capturaban el rayo | `MakeDecorationsTransparentToRay()`: solo los `Selectable` capturan el rayo |
| 36 piezas de válvulas superiores del PHE caían en "Otros" | `^VlvSup_` no coincide con `_07_VlvSup_1_Cuerpo` (guion bajo del exportador) | Reglas sin anclar / con `^_?` |
| Fade del cuestionario cortaba la voz en off y el vídeo se solapaba 11 s | Espera fija de 5 s con un clip de 18 s | Espera calculada desde `clip.length`; además se aguarda el fin real del audio y se libera el `AudioSource` antes de `OnTransitionComplete` |
| Tablero del simulador en blanco | **Regresión introducida por el menú 4**: `PasteurizerWorldCanvas._targetAlpha` arrancaba en 0 y `SetVisible()` solo se llamaba con `showOnlyWhenPinned = true`; el canvas se desvanecía a alpha 0 y quedaba a la vista el quad blanco `PantallaLed` | `_targetAlpha = 1f` y `OnEnable` → `SetVisible(showOnlyWhenPinned ? _hasPinned : true)` |

**Compuerta del tablero (menú 5).** El tablero vive en la escena desde el arranque, así que
aparecía nada más pulsar Play. `PasteurizerSimGate` lo mantiene oculto —y con sus componentes
por frame detenidos, así que tampoco consume— hasta que `GestorEscena3` dispara el nuevo evento
`OnTutorialFinalizado` al terminar el vídeo explicativo. Ahí se enciende y se recoloca frente al
estudiante. No se desactiva el GameObject a propósito: si se hiciera, el propio componente
dejaría de ejecutarse y nadie podría volver a encenderlo.

**Pendiente de decidir:** los muros de `_CollisionWalls` se generaron asumiendo Y-arriba sobre un
modelo Z-arriba rotado, así que quedan girados 90° y atraviesan la máquina. Ya no afectan al
explorador, pero sí a la colisión del jugador. Requiere regenerarlos con la rotación real.

**Configuración de escena que debe hacerse a mano:** en `Canvas_VideoEsc3` → `Video Player`,
cambiar **Audio Output Mode** de `Audio Source` a `Direct`. Hoy apunta al mismo `AudioSource`
que reproduce las voces en off del cuestionario y se lo arrebata.

**Aviso:** con el tablero convertido en panel personal ya no cuelga del televisor, así que
volver a ejecutar el menú 13 crearía un segundo dashboard. Usar el menú 14 (Force Rebuild).

### 4.6 Limpieza pendiente

`Data/arenas` (`.are`, `arena*.json`) es terminología pre-3.0 y ya no aplica. El sample
`Assets/Samples/Viroo Studio/2.6.955` convive con paquetes 3.0.606 y debería reimportarse.

---

## 5. Requisitos de la ficha del proyecto

1. **Registro de tiempo de uso por participante**, con **códigos anónimos** (no nombres reales):
   exigencia del protocolo de bioseguridad y del aval del comité de ética. Debe alimentar los
   instrumentos previstos (SUS, pre/post test, repetibilidad).
2. **Opciones para ajustar la intensidad de la simulación** (confort). Hoy el HUD anclado rígido
   a la cabeza (`PasteurizerWorldCanvas`, modo `HUDAnchor` con `hudSmoothing = false`) va en
   contra de esto.

El cronograma de la ficha terminó el 08/06/2026.

---

## 6. Correcciones de código vigentes (independientes de VIROO)

- Fuga de materiales en el hover (`PasteurizerHoverHandler.cs:280-310`): instancias
  `new Material(...)` nunca destruidas. Usar `MaterialPropertyBlock`, como ya hace bien
  `UnityPasteurizerController.cs:25-49`.
- Dashboard reescribe ~40 textos TMP cada frame → presión de GC constante a 72-90 Hz.
- `ExplodedView.Update` escribe la posición de todas las partes incluso en reposo.
- ~654-860 `MeshCollider` convex cocinados en `Awake` (`PasteurizerPartsRegistry.cs:64-76`).
- NRE latentes: `PasteurizerSimDashboard.cs:169,187`; `GestorCuestionario.cs:245-253`;
  `Camera.main` sin null-check en `FadeOutVR.cs:35` y `GestorCuestionario.cs:191`.
- Los "focus" que mueven `Camera.main` (`PasteurizerHoverHandler.cs:130-145`,
  `PasteurizerSidePanel.cs:176-208`) no funcionan en VR: usar
  `Viroo.Teleport.INetworkTeleportService` o `TeleportSingleAction`.
- `ControladorPuerta`: sin guarda contra doble invocación; `CerrarInstante` no detiene la
  corrutina en curso.
- Slider de caudal desincronizado del motor (2.5 vs máximo 2.0).
- `PasteurizerBuilder.cs:164-179`: se pierde la escala al reemplazar el prefab.
- `autoOcultar.cs`: nombre de archivo no coincide con la clase `AutoOcultar`.
- Unificar codificación a UTF-8; migrar `FindObjectOfType` → `FindFirstObjectByType`.
- Higiene de assets: ~150 MB recuperables. Inicializar control de versiones.

---

## 7. Orden de ejecución sugerido

1. Correr el diagnóstico y resolver lo que reporte (colliders, capas, eventos sin cablear).
2. Definir el Application Identifier y hacer el primer build de prueba.
3. Probar en el visor que los tres objetos de bioseguridad responden al gatillo.
4. Resolver el explorador de 860 piezas (`MultiMeshRayInteraction`).
5. Decidir panel personal vs cuatro estaciones para el SCADA.
6. Requisitos de la ficha (§5) y correcciones de código (§6).

---

## 8. Herramientas de editor añadidas

| Menú | Qué hace |
|---|---|
| `Viroo/Pasteurizador HTST/8. Adaptar a Viroo` | Root + componentes DI, layers, EventSystems, cámaras, aviso de PlayerStart duplicado |
| `Viroo/Adecuacion VIROO/1. Hacer interactuables (individual, sin red)` | Añade `XRSimpleInteractable` + interaction layer Default y verifica collider. **No** añade componentes de red |
| `Viroo/Adecuacion VIROO/2. Preparar Canvases World Space para VR` | Añade `GraphicRaycaster` + `TrackedDeviceGraphicRaycaster` a los canvas World Space |
| `Viroo/Adecuacion VIROO/3. Diagnosticar conformidad VIROO` | Informe en consola de las reglas de build y de los fallos silenciosos de interacción |
