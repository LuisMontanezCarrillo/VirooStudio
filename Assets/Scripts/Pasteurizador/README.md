# Pasteurizador HTST — integración Unity

Toda la lógica del pasteurizador HTST (654 partes con tag/descripción/color)
empaquetada como un prefab reusable + UI 2D para hover/click/panel lateral.

## Estructura

```
Assets/
├── Models/Pasteurizador_HTST/
│   ├── pasteurizador_htst.obj         <- 109 MB, 654 grupos, via Git LFS
│   ├── pasteurizador_htst.mtl         <- 15 materiales master (PALETTE)
│   └── Materials/                     <- materiales URP generados por el builder
├── Resources/Pasteurizador/
│   ├── subsystems.json                <- 23 subsistemas + descripciones especificas
│   └── SubsystemDatabase.asset        <- ScriptableObject generado por el builder
├── Scripts/Pasteurizador/
│   ├── PasteurizationState.cs              data class del estado del proceso
│   ├── UnityPasteurizerController.cs       carga estado JSON, dispara eventos
│   ├── PasteurizerSubsystemDatabase.cs     ScriptableObject + topGroupOf logic
│   ├── PasteurizerMaterialAssigner.cs      asigna materiales URP por nombre
│   ├── PasteurizerPartsRegistry.cs         indexa 654 partes + agrega colliders
│   ├── PasteurizerHoverHandler.cs          raycast VR + mouse, hover/click
│   ├── PasteurizerDescriptionCard.cs       UI tarjeta con tag/desc
│   ├── PasteurizerSidePanel.cs             arbol UI + buscador
│   ├── PasteurizerExplodedView.cs          vista explosionada con lerp
│   └── Editor/
│       ├── PasteurizerBuilder.cs           genera materiales + prefab + reemplazo
│       └── PasteurizerUIBuilder.cs         genera Canvas + Card + Panel UI
└── Prefabs/Pasteurizador_HTST/             generados por el builder
```

## Como usar (1 vez)

Al abrir Unity por primera vez con estos archivos:

1. Espera a que termine de importar el OBJ (toma 1-3 min, son 109 MB con 654 sub-meshes).
2. Menu **Viroo → Pasteurizador HTST → 1. Construir prefab desde OBJ**
   - Genera 15 materiales URP en `Assets/Models/Pasteurizador_HTST/Materials/`
   - Crea `SubsystemDatabase.asset` en `Resources/Pasteurizador/`
   - Crea el prefab `Assets/Prefabs/Pasteurizador_HTST/Pasteurizador_HTST.prefab`
3. Abre la escena destino (ej. `Assets/Scenes/Escena 2.unity`).
4. Menu **Viroo → Pasteurizador HTST → 2. Reemplazar Pasteurizer en escena activa**
   - Busca un GameObject llamado "Pasteurizer", guarda su transform, lo borra,
     instancia el prefab nuevo en el mismo lugar.
5. Menu **Viroo → Pasteurizador HTST → 3. Construir UI (Card + SidePanel)**
   - Genera 5 prefabs de UI: DescriptionCard, SidePanel, GroupRow, PartRow, Canvas.
6. Menu **Viroo → Pasteurizador HTST → 4. Instanciar UI Canvas en escena + conectar**
   - Mete el Canvas en la escena y conecta automaticamente los refs del card/panel
     al `PasteurizerHoverHandler` y `PasteurizerPartsRegistry` del Pasteurizador.

Despues de eso entras a Play y deberias ver:
- Mouse hover sobre cualquier parte -> resalta naranja
- Click izquierdo -> pin amarillo + tarjeta con tag/nombre/descripcion
- Panel lateral derecho con arbol de 23 subsistemas + buscador + toggles
- Boton "Centrar" reencuadra la camara sobre las partes visibles

## Para VR

El componente `PasteurizerHoverHandler` tiene un campo `raySource` (Transform).
Asignale el transform del controlador XR o de la mano dominante; el ray usa
`raySource.forward` con distancia `rayMaxDistance` (default 8m). El mouse
queda como fallback si `alsoUseMouse` esta en true.

Para el "click" en VR puedes asignar `pinKey` (ej. `KeyCode.JoystickButton0`)
o llamar `PasteurizerHoverHandler.SetPinned(part)` desde un evento de XR
Interaction Toolkit. No agrego dependencia dura de XRIT para no acoplar.

## Personalizar materiales

Despues de correr el builder, los 15 materiales viven en
`Assets/Models/Pasteurizador_HTST/Materials/*.mat`. Editalos a gusto (textura
metalica, normalmap, etc.) y el prefab se actualiza automaticamente.

Si cambias las reglas del PALETTE en `PasteurizerMaterialAssigner.ClassifyName`,
corre **Viroo → Pasteurizador HTST → Reaplicar materiales URP** para
re-asignar los materiales del prefab segun las nuevas reglas.

## Performance / optimizacion para VR

- El OBJ se importa con `globalScale = 0.001` (mm -> m), `isReadable = true`
  (para la vista explosionada), `generateSecondaryUV = true` (lightmap).
- El registry agrega `MeshCollider` convex a cada una de las 654 partes
  (necesario para Raycast). Si te falla el batching, considera desactivar
  los colliders en partes que nunca seran clickeables (cables, backings).
- Para reducir draw calls considera activar **Static batching** en las partes
  que no se mueven (todas excepto las que se usan en vista explosionada o
  animaciones). Selecciona el prefab y marca `Static`.

## Datos del proceso

El `UnityPasteurizerController` puede recibir el estado desde el simulador
Python via:
```csharp
controller.LoadStateFromJson(jsonString);
```
Eventos disponibles:
- `OnAlarmChanged(bool)`
- `OnTemperatureOutChanged(float)`
- `OnRecirculationChanged(bool)`

Para test rapido podes asignar un `TextAsset` con un estado de ejemplo
al campo `sampleStateJson` y llamar `LoadSampleState()`.

## Origen de los archivos

- OBJ + MTL + descripciones: generados desde
  `C:/Users/talle/Documents/Simulador pasteurizador/.claude/worktrees/awesome-bohr-e1b736/`
  (FreeCAD `pasteurizador_modelo.py`).
- Las 23 subsistemas y las descripciones especificas fueron portadas
  del `visor.html` original (Three.js -> Unity).
- El `.mtl` se genero a partir de la PALETTE de visor.html (15 reglas).
