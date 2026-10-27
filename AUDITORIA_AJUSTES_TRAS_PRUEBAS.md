# Auditoría de los ajustes tras las pruebas con estudiantes

**Proyecto:** PIZCBC292025 · Simulador inmersivo de pasteurización · Sala VIROO
**Pruebas auditadas:** 29 de septiembre de 2026 · 11 estudiantes de Ingeniería de Alimentos
**Fecha de la auditoría:** 27 de octubre de 2026
**Rama:** `bloque-a-ajustes-legibilidad-audio`

Todo lo que se afirma aquí sobre el estado del proyecto está leído del repositorio,
no de los documentos de planificación. Las cifras de los instrumentos están
recontadas sobre las respuestas, no copiadas de informes anteriores.

---

## 1. Veredicto

**De los ocho hallazgos de sala, hay tres resueltos en código, uno resuelto a
medias y cuatro sin tocar. Ninguno de los resueltos está verificado con las gafas
puestas.**

Esa última frase es la más importante del documento. Las pruebas del 29 de
septiembre fallaron precisamente con código que funcionaba en el editor, y una
auditoría del repositorio no puede sustituir una sesión en la sala.

| | Hallazgos |
|---|---|
| ✅ Resuelto en código, pendiente de comprobar en la sala | 3 |
| 🟡 Resuelto a medias | 1 |
| ⬜ Sin tocar | 4 |
| ⛔ No corregible dentro del proyecto | 1 (añadido por los instrumentos) |

---

## 2. Lo que se tocó de verdad después de las pruebas

Evidencia: historial de la rama.

El 29 de septiembre se hicieron las pruebas. Desde esa fecha **solo hay un commit**,
más los cambios sin confirmar del árbol de trabajo:

| Fecha | Commit | Qué toca |
|---|---|---|
| 2026-10-14 | `85f6ad3` | `GuiaDeLaMarca.cs`, `ReunionEnElCentro.cs`, `GatilloComoSelect.cs` y dos documentos |
| sin confirmar | — | `Escena 1.unity` (cableado del reto 3 y las esperas), la guía, un atlas de TextMesh Pro |

Todos los demás commits (24 al 28 de septiembre) son **anteriores** a las pruebas.
Es decir: el cableado de reuniones, avances en grupo y marcas **ya estaba puesto
el día de las pruebas, y aun así falló**. Eso descarta que el problema fuera
cableado que faltara, y es lo que orientó el diagnóstico.

Un dato lo confirma del todo. El aro del suelo y el cartel nacieron en el **mismo
commit y el mismo archivo** (`78e84b8`, 28 de septiembre). Usted vio el cartel y
no vio el aro. Luego el componente corría en el visor, y lo único que fallaba era
cómo se dibujaba el disco.

---

## 3. Las ocho notas de sala, una por una

### Nota 1 — «Los participantes con gafas no vieron los círculos tras el reto 1»

**Estado: ✅ resuelto en código · ⚠️ sin verificar en la sala**

Qué había: el aro era un cilindro primitivo (`GameObject.CreatePrimitive`) con el
material manipulado en ejecución para volverlo translúcido.

Qué hay ahora, leído de `Assets/Scripts/Multiusuario/GuiaDeLaMarca.cs`: el aro es
un `Canvas` en espacio de mundo con un `RawImage` sobre una `Texture2D` generada
por código (`DibujarAro`, 256×256). Ningún material se toca en ejecución.

Por qué debería bastar: el proyecto renderiza en **Single Pass Instanced**
(`m_renderMode: 1`, siete veces en `Assets/XR/Settings/Open XR Package
Settings.asset`). Un material manipulado por código es justo lo que se ve bien en
la vista mono del editor y puede fallar en estéreo instanciado; además, las
variantes de shader que solo se piden en ejecución se pueden eliminar al compilar.
El cartel no sufre ninguno de los dos problemas porque es un lienzo de interfaz, y
de ese camino tenemos constancia directa de que funciona en el visor. El aro ahora
va por ese mismo camino.

**Lo que esto no es.** Es un rodeo, no una corrección de la causa. El modo estéreo
sigue en Single Pass Instanced. Si en la sala aparecen más objetos invisibles,
habrá que tocar el modo y no el objeto.

De paso: la versión anterior declaraba y usaba un campo `_centro` que **ningún
camino del código llegaba a crear**. Era resto del haz vertical que se añadió y se
retiró el 28 de septiembre. Siempre valió nulo. La reescritura lo eliminó.

### Nota 2 — «El cuestionario aún exige el botón lateral; debería ir con el gatillo»

**Estado: ✅ resuelto en código · ⚠️ sin verificar en la sala**

Causa encontrada: `GatilloComoSelect` solo reasignaba `selectInput`. Los botones
del cuestionario y del tablero no van por ahí, van por `uiPressInput`, que es por
donde pasa toda la interfaz de Unity. La escena tiene diez
`TrackedDeviceGraphicRaycaster`, o sea diez lienzos que dependían de esa entrada
sin reasignar.

Qué hay ahora en `Assets/Scripts/Pasteurizador/GatilloComoSelect.cs`: se reasignan
`selectInput`, `activateInput` y `uiPressInput`, este último tanto en
`XRRayInteractor` como en `NearFarInteractor`.

Segundo defecto corregido: el componente se apagaba solo a los 25 segundos. En una
sala VIROO los participantes entran cuando entran, y quien llegaba tarde se
quedaba con el grip sin que nadie se enterara. Ahora rastrea toda la sesión, cada
2 segundos.

Tercero: ahora escribe en consola también el caso «esta acción **ya** estaba en el
gatillo», con la lista de bindings. Sin eso, una acción que no cambia no deja
rastro y no hay forma de saber desde fuera qué entrada usa de verdad el rig de
VIROO. Esa línea de consola es la prueba que hay que recoger en la sala.

**Respaldo en el instrumento:** un estudiante escribió *«los mandos de las manos
no tienen mucha sensibilidad»*.

### Nota 3 — «La transición a la escena 3 es caótica; no funcionan los marcadores del suelo»

**Estado: ✅ resuelto en código y cableado · ⚠️ sin verificar en la sala**

Esta nota y la nota 1 eran el mismo defecto visto dos veces: marca invisible →
nadie llega → salta la espera máxima → el teletransporte de VIROO conserva las
posiciones relativas → la dispersión que ya había se arrastra al reto 3.

Dos cosas cambiaron:

1. El aro se ve (nota 1).
2. Se añadió una reunión **después** del salto. `ReunionEnElCentro` tiene ahora un
   campo `soloReunir`: recoloca a cada participante en su marca sin volver a
   saltar, y un evento `AlTerminarLaReunion` para encadenar lo que venga.

Cableado comprobado en `Assets/Scenes/Escena 1.unity`:

- `Root / Reunion_Llegada_Reto3` con `soloReunir: 1` y `avance` vacío ✓
- Marcas en el orden `Punto_Inicio_Escena3_B`, `_C`, el padre, `_D` ✓
- `Convocar()` enganchado en `Avance_Reto2 → AvanceEnGrupo → DespuesDelSalto`,
  después de `AclararAhora` ✓

El orden de las marcas no es el obvio, y es deliberado. Con dos visores los
participantes aterrizan a 1,02 m y 0,69 m del ancla, y sus puntos más cercanos son
`_B` (a 0,12 m) y `_C` (a 0,21 m). El orden obvio (padre, `_B`, `_C`, `_D`) les
habría pedido caminar 1,02 m y 1,59 m **cruzándose a ciegas**.

También se alinearon las esperas, que estaban descuadradas: los dos
`ReunionEnElCentro` de los retos 1 y 2 esperaban 60 segundos mientras sus
`AvanceEnGrupo` esperaban 45. Ahora las cuatro están en 45. La reunión nueva del
reto 3 está en 20, porque no es una puerta que detenga al grupo.

### Nota 4 — «El reto 3 quedó fuera de la malla, sobre todo con quien nunca ha usado RV»

**Estado: 🟡 atacada la causa probable, sin medir**

Lo que sí está hecho: la reunión posterior al salto (nota 3) recoloca a cada
participante en una marca conocida dentro del área. Si la dispersión era la causa,
esto la corta.

Lo que no está hecho: **nadie ha medido** que las cuatro marcas de llegada queden
dentro del área útil con cuatro visores. La sala mide 6,95 × 13,10 m. Con dos
participantes la cuenta sale; con cuatro no lo he verificado, y las marcas `_D` y
el padre quedan sin usar en una sesión de dos.

Y hay un matiz que conviene no perder. Una estudiante escribió *«nunca vi la malla
azul, para estar más segura»*. Su queja es la contraria a la nota: no es que se
saliera del área, es que **el límite no se le mostró**. Las dos apuntan al mismo
control, y ese control es de la configuración del visor y de la sala, no del
proyecto. Merece una decisión explícita antes de la próxima sesión.

**Contexto del instrumento:** **6 de los 11** estudiantes nunca habían usado un
visor de realidad virtual. No es un caso raro: es la mayoría.

### Nota 5 — «Los participantes se pierden; hacen falta orientaciones visuales»

**Estado: ⬜ sin tocar**

No hay nada en el repositorio que atienda esto. Lo único que se acerca es el
cartel de `GuiaDeLaMarca`, que solo aparece durante las transiciones entre retos y
dice tres cosas: «Ve a la marca del suelo · te faltan X m», «Gira hacia la
derecha/izquierda» y «Quédate aquí · esperando al resto». Durante el reto, que es
cuando se pierden, no hay nada.

Esto era el bloque B1 del plan avalado, y sigue pendiente de que usted aporte la
lista de pasos y qué debe decir cada uno.

**Respaldo en el instrumento, y es el más repetido:**

- *«Más señalización para moverse dentro del simulador, ya que se siente perdido
  al no conocer para dónde dirigirse o qué hacer»*
- *«Siento que debe ser un poco más guiado las instrucciones»*
- *«Tiene que ser más intuitivo»*

### Nota 6 — «No se reconocen los botones de control del pasteurizador 2D»

**Estado: ⬜ sin tocar**

`PasteurizerSimDashboard.cs` declara nueve `Button` (energía, arranque, parada,
modo, llenado, bomba caliente, bomba fría, bomba de leche y reinicio de lote). Son
botones de Unity de verdad; el problema es que no se **leen** como botones.

No se parte de cero: el tablero ya tiene un indicador de seis pasos
(`stepDots[6]`), una guía por voz (`PasteurizerVoiceGuide`) y una ventana de
arranque (`PasteurizerStartOverlay`). Falta el tratamiento visual.

Era el bloque D1.

### Nota 7 — «Debería haber numeración sobre el modelo 3D»

**Estado: ⬜ sin tocar**

`PasteurizerPartsRegistry` indexa las piezas en diccionarios por nombre y por
subsistema. **No existe ningún orden** en la estructura de datos, de modo que esto
no es solo añadir etiquetas: hay que decidir y guardar la secuencia esperada.

Era el bloque D2.

**Respaldo en el instrumento, casi literal:** *«En el equipo sería importante tener
numerado cada uno de los componentes y poder explorar los componentes en el orden
del diagrama presentado anteriormente»*.

### Nota 8 — «El tablero no es individual: unas gafas resuelven y las demás ven»

**Estado: ⬜ sin tocar**

No hay ninguna lógica de turnos ni de propiedad sobre el tablero en el
repositorio. `SesionViroo` expone quién está conectado, el índice del jugador
local y si uno es el cliente designado, pero nada de eso se usa para repartir el
mando del tablero. Lo único que tiene turnos es `NarradorUnico`, y son turnos de
voz.

Conviene separar dos cosas. Que el tablero sea **compartido** es inherente a VIROO:
es un objeto de la sala, igual que el pasteurizador sería uno solo en una planta
real. Lo que sí se puede hacer, y es lo que usted eligió, son **turnos obligados**:
que cada participante tenga que tomar el mando en algún momento.

Era el bloque C.

---

## 4. Lo que dicen los instrumentos, recontado

11 respuestas en cada instrumento.

### Aprendizaje (aciertos sobre 11)

| Pregunta | Antes | Después |
|---|---|---|
| Orden de las etapas del tratamiento térmico | 4 | **8** |
| Combinación temperatura/tiempo en HTST | 6 | **10** |
| Por qué enfriar rápido | 10 | 11 |
| Propósito del protocolo de bioseguridad | 10 | 11 |

**La experiencia enseñó, y eso es lo que el proyecto tenía que demostrar.** Las dos
preguntas con margen real subieron con claridad, y lo hicieron **a pesar** de una
sesión que hubo que detener.

> ⚠️ Las cifras de la diapositiva 9 de la presentación siguen sin corregir: dicen
> 5→9 y 7→10, y los datos dan 4→8 y 6→10. Lo reporté y quedó sin respuesta.

### Valoración (escala de 1 a 7, promedio de 11 respuestas)

| Ítem | Promedio |
|---|---|
| Las funciones cubren lo que necesitaba | 5,36 |
| El simulador me ayudó a comprender el proceso | 5,45 |
| El simulador es fácil de usar | 5,00 |
| **Los sonidos me ayudaron a saber en qué estado estaba el proceso** | **4,55** |

**El sonido es el ítem peor valorado, y es el que sigue sin tocarse.**

El repositorio explica por qué: **no existe ningún AudioMixer en el proyecto**. No
hay ningún archivo `.mixer`, y los doce `AudioSource` de la escena tienen
`OutputAudioMixerGroup: {fileID: 0}`, es decir, ninguno. Todo suena al mismo nivel,
sin buses y sin posibilidad de bajar la planta cuando habla la voz.

Eso es exactamente lo que describen los estudiantes:

- *«Mucho ruido de las máquinas, y poco sonido de la explicación»*
- *«Bajo volumen»*
- *«No se escuchó muy bien»*
- *«Falta más realismo en el tema de los sonidos»*

Era el bloque B2.

### Molestias

| Respuesta | Estudiantes |
|---|---|
| Ninguna | 4 |
| Leve | 6 |
| **Intensa** | **1** |

El caso intenso reportó *«dolor de cabeza intenso y sensación de mareo»*. Otros dos
atribuyen la molestia al peso y la presión del visor, no a la experiencia.

**Nada en el repositorio limita la duración de la sesión.** Sigue abierta la
decisión de poner un tope de 20 minutos. Con un caso intenso sobre once, esto no es
un ajuste de comodidad.

---

## 5. Lo que no es corregible dentro del proyecto

**La imagen borrosa.** Dos estudiantes la mencionan: *«mejoraría el tema visual ya
que se ve borroso»* y *«mejoraría la resolución de los vídeos y textos que están
como presentación, se complica un poco la lectura»*.

El origen está en la transmisión inalámbrica del PC al visor por HTC Business
Streaming, que comprime la imagen. Ningún cambio en el proyecto la descomprime.

Lo que sí se puede hacer es diseñar contra ello: cuerpos de letra mayores, más
contraste, menos texto por pantalla y no depender de detalle fino en los vídeos.
Era el bloque D3, y tampoco está hecho.

---

## 6. Resumen en una tabla

| # | Hallazgo | Estado | Evidencia en el repositorio |
|---|---|---|---|
| 1 | Círculos invisibles con gafas | ✅ código | `GuiaDeLaMarca.cs` reescrito como interfaz |
| 2 | Cuestionario con botón lateral | ✅ código | `GatilloComoSelect.cs` reasigna `uiPressInput` |
| 3 | Transición al reto 3 caótica | ✅ código + cableado | `Reunion_Llegada_Reto3` en la escena |
| 4 | Reto 3 fuera de la malla | 🟡 parcial | la misma reunión; sin medir con 4 visores |
| 5 | Los participantes se pierden | ⬜ | nada |
| 6 | Botones 2D no se reconocen | ⬜ | nada |
| 7 | Falta numeración en el 3D | ⬜ | nada; el registro no tiene orden |
| 8 | El tablero es grupal | ⬜ | nada; `SesionViroo` no se usa para turnos |
| 9 | Sonido, ítem peor valorado (4,55/7) | ⬜ | no existe AudioMixer |
| 10 | Imagen borrosa | ⛔ | fuera del proyecto; mitigable por diseño |
| 11 | Una molestia intensa sobre once | ⬜ | nada limita la duración |

---

## 7. Lo que haría falta para cerrar la auditoría

1. **Una sesión corta en la sala con dos visores**, con la consola visible. Es lo
   único que convierte los tres ✅ en hechos. Los datos a recoger están en
   `BLOQUE_A_CABLEADO_Y_COMPROBACION.md`, parte 3.
2. **Decidir el modo estéreo.** Hoy sigue en Single Pass Instanced y la guía de
   VIROO pide Multi-Pass. La recomendación sigue siendo no tocarlo hasta medir;
   pero si en la sala aparece otro objeto invisible, los hechos ya habrán decidido.
3. **Decidir sobre el límite del área** y sobre si se le muestra o no al
   participante.
4. **Decidir el tope de duración de la sesión.**
5. **Corregir la diapositiva 9** de la presentación.
