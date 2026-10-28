# Bloque B2 · Audio: mezclador y calidad

**Qué atiende:** el ítem peor valorado del instrumento (los sonidos, **4,55 sobre 7**)
y cuatro comentarios de los estudiantes.

> *«Mucho ruido de las máquinas, y poco sonido de la explicación»*
> *«Bajo volumen»* · *«No se escuchó muy bien»* · *«Falta más realismo en el tema de los sonidos»*

---

## Parte 0 · Qué encontré al abrir el audio del proyecto

Dos cosas, y la segunda no estaba en el plan.

**Primera: no había mezclador.** Ningún archivo `.mixer`, y los doce `AudioSource`
de la `Escena 1` con `OutputAudioMixerGroup` vacío. Todo sonaba al mismo nivel, sin
buses, sin forma de bajar una cosa sin bajar las demás. Eso ya lo sabíamos.

**Segunda, y es la que explica el comentario de los estudiantes.** Mire estas dos
líneas del proyecto, una al lado de la otra:

- `NarradorUnico` ya arbitra quién habla. Cuando una narración toma la palabra,
  **para** las demás narraciones y **pausa** los vídeos.
- `PasteurizerFxController` crea **siete fuentes de sonido en tiempo de ejecución**,
  de las cuales **cinco son bucles que pueden sonar a la vez**: proceso, bomba de
  producto, bomba de agua, caldera y vapor.

El árbitro no conoce esos siete. No son voces, así que nunca los tocó. La narración
no competía con otra narración: competía con cinco máquinas a la vez, todas en 2D y
todas al mismo volumen en cualquier punto de la sala. Es literalmente *«mucho ruido
de las máquinas, y poco sonido de la explicación»*.

Y hay un detalle que condiciona la solución: **esas siete fuentes no existen en la
escena**. Se crean al arrancar. No hay ningún campo del Inspector donde asignarles
un bus. Cualquier solución que consista en abrir la escena y asignar grupos a mano
deja fuera justo las que hacen el ruido.

---

## Parte 1 · Lo que ya está hecho, sin que usted toque nada

### `Assets/Scripts/MezcladorDeAudio.cs` (nuevo)

Hace dos cosas:

1. **Reparte** cada fuente de sonido en uno de tres buses, rastreando la escena cada
   2 segundos. Así entran también las siete que se crean en ejecución, y las de los
   participantes que llegan tarde.
2. **Agacha la planta mientras alguien habla.** Baja 9 dB en 0,15 s y vuelve en 0,70 s.
   Rápido al bajar para no comerse la primera palabra; lento al subir para que la
   máquina no vuelva de golpe entre frase y frase.

**Funciona aunque usted no cree el mezclador.** Es la lección del aro invisible: un
mecanismo que depende de que alguien se acuerde de cablear su parte falla en
silencio y se descubre en la sala. Sin mezclador, el agache se aplica directamente
sobre el volumen de cada fuente de planta. Se controla peor, pero la mejora que más
pesa en el instrumento no se queda esperando a un campo vacío. El componente dice en
consola, al arrancar, en cuál de los dos modos está.

### `Assets/Scripts/NarradorUnico.cs` (una línea)

Se añadió `HayAlguienHablando()`, que es lo que el mezclador pregunta. La regla de
quién tiene la palabra ya se decidía en `NarradorUnico`, incluidas las narraciones
que arrancan solas desde un UnityEvent. Duplicarla en otro sitio sería tener dos
verdades que acabarían discrepando.

**Ambos archivos compilan.** Verificado con el compilador de Unity 6000.0.49f1
(Roslyn), sobre todos los scripts del proyecto: 0 errores.

---

## Parte 2 · Lo que tiene que hacer usted

Son seis pasos. El más largo son tres minutos.

### Paso 1 · Crear la carpeta y el mezclador

1. En la ventana **Project**, clic derecho sobre **`Assets`** → **Create** → **Folder**.
2. Llámela exactamente **`Audio`**.
3. Clic derecho sobre esa carpeta **`Assets/Audio`** → **Create** → **Audio Mixer**.
4. Llámelo exactamente **`MezcladorPrincipal`**.

**Qué debe ver:** un archivo `MezcladorPrincipal.mixer` dentro de `Assets/Audio`,
con un icono de deslizadores.

### Paso 2 · Abrir la ventana del mezclador

Menú **Window** → **Audio** → **Audio Mixer**.

**Qué debe ver:** una ventana con cuatro paneles a la izquierda (*Mixers*,
*Snapshots*, *Groups*, *Views*). En **Groups** hay un solo grupo: **Master**.

Si la ventana aparece vacía, haga doble clic sobre `MezcladorPrincipal.mixer` en el
Project.

### Paso 3 · Crear los tres grupos

Los tres cuelgan de **Master**, no unos de otros.

1. En el panel **Groups**, haga clic en **Master** para seleccionarlo.
2. Pulse el **`+`** de la cabecera del panel **Groups**.
3. Escriba el nombre y pulse Enter.
4. **Vuelva a hacer clic en Master** antes de crear el siguiente. Si no, el grupo
   nuevo cuelga del anterior.

Cree estos tres, con estos nombres exactos:

| Nombre | Qué va a llevar |
|---|---|
| `Voz` | narraciones, la guía del tablero, los vídeos con voz |
| `Planta` | las cinco máquinas, los golpes de arranque y paro, el ambiente de sala |
| `Interfaz` | clics, errores, avisos de los lienzos |

**Qué debe ver:** en **Groups**, `Master` con tres hijos indentados al mismo nivel:
`Voz`, `Planta`, `Interfaz`. Y en la zona central, cuatro columnas con su deslizador.

> ⚠️ **Si `Planta` quedó colgando de `Voz`**, arrástrelo en el panel **Groups** hasta
> soltarlo sobre `Master`. La jerarquía importa: un hijo de `Voz` se agacharía a sí
> mismo.

### Paso 4 · Exponer los cuatro parámetros

Esto es lo que permite que el script mueva los niveles. Es el paso donde se cometen
los errores, porque **un nombre mal escrito no da ningún error**: el nivel
simplemente no hace nada.

Para cada uno de los cuatro grupos, incluido Master:

1. Haga clic en el grupo en el panel **Groups**.
2. Mire el **Inspector**. Arriba hay una sección **Attenuation** con una propiedad
   **Volume**.
3. **Clic derecho sobre la palabra `Volume`** (sobre la etiqueta, no sobre la barra).
4. En el menú que sale, elija **`Expose 'Volume (of <grupo>)' to script`**.

Luego renómbrelos:

5. Arriba a la derecha de la ventana del Audio Mixer hay un desplegable que ahora
   dice **`Exposed Parameters (4)`**. Haga clic.
6. Aparecen los cuatro, todos llamados `MyExposedParam`. **Doble clic** sobre cada
   nombre y escriba el que toca.

| Grupo | Nombre exacto del parámetro |
|---|---|
| Master | `VolumenGeneral` |
| Voz | `VolumenVoz` |
| Planta | `VolumenPlanta` |
| Interfaz | `VolumenInterfaz` |

Sin tildes, con las mayúsculas tal cual. `volumenVoz` o `Volumen Voz` no valen.

**Qué debe ver:** el desplegable dice `Exposed Parameters (4)` y al abrirlo salen los
cuatro nombres de la tabla.

> Si se equivoca al escribir uno, no pasa nada: el componente lo detecta al arrancar
> y escribe en consola **en rojo** qué parámetro no encontró, nombrándolo. Es el
> único aviso que es un error y no una advertencia, justamente porque es un fallo
> que de otro modo sería mudo.

### Paso 5 · Poner el componente en la escena

1. Abra **`Assets/Scenes/Escena 1.unity`**.
2. En la **Hierarchy**, clic derecho sobre **`Root`** → **Create Empty**.
3. Llame al objeto nuevo exactamente **`Mezclador_Audio`**. Queda en
   **`Root / Mezclador_Audio`**, junto a `Ajuste_GatilloComoSelect`,
   `Mando_Moderador` y `Reinicio_Experiencia`, que es donde viven los componentes
   que valen para toda la sesión.
4. Con `Mezclador_Audio` seleccionado, en el Inspector pulse **Add Component** y
   busque **`Mezclador de audio`**.

Ahora rellene los cuatro campos de arriba, **arrastrando desde la ventana Project y
desde el Audio Mixer**:

| Campo del Inspector | Qué arrastrar | De dónde |
|---|---|---|
| **Mezclador** | `MezcladorPrincipal` | Project, `Assets/Audio` |
| **Grupo Voz** | `Voz` | despliegue el `.mixer` en Project con la flechita ▶ |
| **Grupo Planta** | `Planta` | lo mismo |
| **Grupo Interfaz** | `Interfaz` | lo mismo |

> **Dónde están los grupos.** En la ventana **Project**, el archivo
> `MezcladorPrincipal.mixer` tiene una **flecha ▶ a la izquierda**. Al desplegarla
> aparecen los cuatro grupos como sub-activos, y esos son los que se arrastran. No se
> arrastran desde la ventana del Audio Mixer.

**Los demás campos déjelos como vienen.** Están puestos con un motivo:

| Campo | Valor | Por qué |
|---|---|---|
| Param General / Voz / Planta / Interfaz | los cuatro nombres del paso 4 | ya vienen escritos |
| Nivel General | `0` | referencia |
| Nivel Voz | `+2` | *«bajo volumen»* y *«no se escuchó muy bien»* |
| Nivel Planta | `-6` | hasta cinco bucles se suman |
| Nivel Interfaz | `-3` | los clics no deben competir |
| Agache | `9` | bajada clara sin apagar la máquina, que también informa |
| Segundos De Bajada | `0.15` | no comerse la primera palabra |
| Segundos De Subida | `0.70` | que no vuelva de golpe entre frases |
| Segundos Entre Rastreos | `2` | igual que `GatilloComoSelect` |
| Siempre Voz / Planta / Interfaz | vacíos | solo para corregir una clasificación mal hecha |
| Reasignar Siempre | desmarcado | respeta lo que esté puesto a mano |
| **Log** | **marcado** | es lo que le va a decir si funcionó |

**No hay que cablear nada más.** En concreto:

- **No** hay que tocar los doce `AudioSource` de la escena. El componente los reparte.
- **No** hace falta `Send`, `Receive` ni el efecto `Duck Volume` del mezclador. El
  agache lo hace el script, que es donde se puede medir y ajustar. Esa es la parte
  que más se suele cablear mal.
- **No** hay que añadirlo a la lista del reinicio entre tandas: `ReinicioDeExperiencia`
  busca por su cuenta todo lo que sepa reiniciarse.

### La Escena 2 no hace falta

La `Escena 2` tiene siete `AudioSource` propios, incluido `Sonido_Ambiente_lobby`, y
una versión anterior de esta guía pedía repetir el paso 5 allí. **No hace falta.**

`ProjectSettings/EditorBuildSettings.asset` la tiene **desactivada** (`enabled: 0`), y
todas las llamadas a `SceneManager` del proyecto están en scripts de Editor: ninguna
en ejecución. Nadie la carga, así que sus fuentes de sonido nunca suenan en la sesión.

Si algún día entra en el build, entonces sí: objeto vacío `Mezclador_Audio`,
componente, y los mismos cuatro campos apuntando al mismo `MezcladorPrincipal`.

---

## Parte 3 · Comprobación en el editor, sin visor

Esto se puede hacer en dos minutos y detecta casi todos los errores antes de bajar a
la sala.

1. Abra la `Escena 1` y pulse **Play**.
2. Mire la **Console** desde el primer segundo.

**La primera línea le dice en qué modo está:**

| Lo que lee | Qué significa |
|---|---|
| `Con mezclador: los niveles y el agache van en decibelios sobre los buses.` | Todo bien, siga |
| `SIN mezclador (falta el AudioMixer o alguno de los tres grupos)` | Falta algo del paso 5. Revise los cuatro campos |
| `El mezclador no tiene expuesto el parametro 'X'` **en rojo** | El paso 4. Compare letra por letra |

**Después verá una línea por cada fuente**, con su ruta completa y su bus:

```
[MezcladorDeAudio] 'Root/.../RoomTone_PlantaPiloto' -> Planta
[MezcladorDeAudio] 'Root/.../SND_Click_Lavamanos' -> Interfaz
[MezcladorDeAudio] 'Root/.../VozOff2' -> Voz
```

**Qué revisar en esa lista:**

- Las que digan `FX_ProcLoop`, `FX_PumpLoop`, `FX_WaterLoop`, `FX_BoilerLoop`,
  `FX_VaporLoop`, `FX_OneShot` y `FX_Touch` tienen que ir a **Planta**. Son las siete
  que se crean en ejecución y **solo aparecen cuando el simulador arranca**, no al
  cargar la escena.
- Si alguna advertencia dice *«no tiene clip y su nombre no dice a qué bus va»*,
  arrástrela al array que corresponda (`Siempre Voz`, `Siempre Planta` o
  `Siempre Interfaz`). Esa es la lista para las excepciones, y debería quedar casi
  vacía.

3. **Vea el agache funcionando.** Con el Play corriendo, abra la ventana del Audio
   Mixer: el deslizador de **Planta** baja cuando empieza una narración y sube
   despacio al acabar. Es lo que se va a oír.

4. **Ajuste en vivo.** Todos los niveles se pueden mover durante Play y se oyen al
   instante. Si al final se queda con otros valores, **anótelos**: al salir de Play
   los campos del Inspector vuelven a los suyos, pero los del mezclador no se guardan.

---

## Parte 4 · El paso opcional que más mejora la inteligibilidad

Un **compresor** en el bus de voz. Es lo que hace que la narración se entienda por
encima de la planta sin tener que subirla hasta que moleste, y ayuda especialmente
con la compresión de la transmisión inalámbrica.

1. En la ventana del Audio Mixer, haga clic en el grupo **`Voz`**.
2. En la parte de abajo de su columna, pulse **`Add…`** → **`Compressor`**.
3. Póngale estos valores en el Inspector:

| Parámetro | Valor |
|---|---|
| Threshold | `-18 dB` |
| Ratio | `3` |
| Attack | `10 ms` |
| Release | `150 ms` |
| Make up gain | `+4 dB` |

**Cómo saber si se pasó:** si la voz suena plana, apretada o como si respirara,
**suba el Threshold** hacia `-12`. Si sigue costando entenderla sobre la planta,
bájelo hacia `-24`. Es el único ajuste de esta guía que hay que juzgar de oído, y
conviene hacerlo con el visor puesto y no con los altavoces del PC.

---

## Parte 5 · Lo que esto NO arregla, y qué haría falta

**«Falta más realismo en el tema de los sonidos».** Este comentario no lo resuelve el
mezclador y conviene no pretender que sí.

Las siete fuentes de la planta se crean con **`spatialBlend = 0`**, es decir, sonido
plano: suenan igual en cualquier punto de la sala
(`Assets/Scripts/Pasteurizador/Simulator/PasteurizerFxController.cs`, línea 83).
En una sala donde se camina a escala 1:1, eso es una pérdida doble: no es realista, y
sobre todo **no informa de qué máquina está haciendo qué**, que es exactamente lo que
pregunta el ítem peor valorado del instrumento.

**Por qué no lo he cambiado.** Las siete fuentes cuelgan todas del mismo objeto, no
de sus máquinas. Ponerlas en 3D tal cual solo haría que *todo* el sonido de planta se
desvaneciera al alejarse de un punto cualquiera, que no es realismo: es un error nuevo.
Hacerlo bien es colocar cada fuente sobre su equipo en el modelo —caldera, bomba de
producto, bomba de agua, intercambiador, tanque— y ajustar cada distancia de caída.

Eso es trabajo de escena, necesita decidir qué objeto es cada máquina, y merece ser su
propia tarea con su propia comprobación. **Lo dejo propuesto, no hecho.** Si lo quiere,
dígamelo y preparo el componente y la lista de equipos.

---

## Resumen

| Paso | Quién | Tiempo |
|---|---|---|
| Código del mezclador y del agache | hecho, compila | — |
| Crear el `.mixer` y los tres grupos | usted | 2 min |
| Exponer los cuatro parámetros | usted | 2 min |
| Poner el componente en la Escena 1 | usted | 2 min |
| Comprobar en Play con la consola | usted | 2 min |
| Compresor en el bus de voz | opcional | 1 min |
| Sonido 3D por máquina | propuesto, no hecho | — |
