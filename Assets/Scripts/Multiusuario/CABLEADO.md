# Bloque C · Guía detallada de lo que hay que hacer en Unity

Este documento es para seguirlo con Unity abierto. Cada paso dice exactamente qué objeto
tocar, qué escribir y qué debe pasar después.

---

## Antes de nada: qué está hecho y qué le toca a usted

**Lo que ya está hecho y no hay que tocar.** Son nueve archivos de código, ya en el
repositorio y ya compilando:

| Archivo | Qué hace |
|---|---|
| `IReiniciable.cs` | El contrato que permite a un componente saber volver a su estado inicial |
| `ReinicioDeExperiencia.cs` | Orquesta el reinicio entre tandas |
| `FotoDeLaEscena.cs` | Guarda el estado de la escena al arrancar y lo devuelve |
| `ReunionEnElCentro.cs` | Manda a cada participante a su marca antes del salto |
| `AvanceEnGrupo.cs` | Hace que el grupo salte junto, con el teletransporte de VIROO |
| `SesionViroo.cs` | Sabe cuánta gente hay y a quién le toca ejecutar |
| `FundidoDePantalla.cs` | El fundido a negro, uno solo para toda la experiencia |
| `NarradorUnico.cs` | Turnos de la voz, para que no se corten las frases |
| Trece componentes existentes | Modificados para saber reiniciarse solos |

**Lo que le toca a usted.** Solo esto, y solo porque no se puede hacer de otra forma: las
acciones de VIROO llevan identificador, inyección de dependencias y validadores de editor, así
que **no se pueden crear editando el archivo de escena**. Hay que añadirlas desde el
Inspector.

| Parte | Qué va a crear | Tiempo aproximado |
|---|---|---|
| 1 | Cuatro objetos vacíos que son las marcas del suelo | 5 min |
| 2 | El avance del reto 1 al reto 2 | 20 min |
| 3 | El avance del reto 2 al reto 3 | 15 min |
| 4 | El reinicio entre tandas | 15 min |
| 5 | El botón del moderador | 5 min |

**Nada de esto rompe lo que hay.** Mientras no termine, la experiencia sigue funcionando como
hoy. Los caminos antiguos siguen ahí y solo se retiran al final, en la parte 7, cuando lo
nuevo esté probado.

---

## Antes de empezar

1. Abra **`Assets/Scenes/Escena 1.unity`**.
2. Abra la consola (**Window → General → Console**) y déjela visible. Todos los componentes
   nuevos escriben en ella lo que hacen.
3. Compruebe que no hay errores rojos de compilación. Si los hay, pare y avíseme.

**Un aviso sobre los nombres.** En esta escena hay un objeto que se llama **`Canvas _Esc2`**,
con un espacio entre `Canvas` y `_Esc2`. No es una errata mía. Cuando lo busque en la
jerarquía, escriba `Esc2` y no `Canvas_Esc2`, porque con el guion bajo pegado no aparece.

---

## Parte 1 · Las marcas del suelo

Son los sitios a los que se pide a cada participante que vaya antes de que el grupo salte.

**Por qué marcas separadas y no un círculo común.** En una sala VIROO la posición virtual y la
física son la misma. Juntar a los dos participantes en el mundo virtual es juntarlos de
verdad, con dos personas a ciegas dentro de sendos visores. Cada uno tiene su marca y las
marcas están separadas.

**Y por qué además conviene.** El teletransporte de VIROO mueve al grupo conservando las
posiciones relativas. Si el salto arranca con la gente desperdigada, esa dispersión viaja al
reto siguiente. Saliendo de marcas conocidas, la separación de llegada también es conocida.

### 1.1 Marcas del reto 1

En la jerarquía, sobre el objeto **`Root`**: clic derecho → **Create Empty**. Repita para
crear dos, y póngales estos nombres y estas posiciones exactas (Inspector, componente
`Transform`, campo `Position`):

| Nombre del objeto | Position X | Y | Z |
|---|---|---|---|
| `Marca_Reto1_A` | **-3.08** | **0** | **-7.26** |
| `Marca_Reto1_B` | **-1.08** | **0** | **-7.26** |

Esas coordenadas no son arbitrarias. `RoomCenterStart` está en (-2.08, 0, -7.26): ahí es donde
cae el centro de la sala física al arrancar. Las dos marcas quedan a un metro de ese centro,
una a cada lado, y a **dos metros la una de la otra**. La sala mide 10,71 × 6,53 m, así que
hay 5,35 m de holgura en X y 3,26 m en Z: caben de sobra.

### 1.2 Marcas del reto 2

Estas van alrededor del destino del salto anterior, **no** alrededor de `RoomCenterStart`,
porque después del primer salto el centro de la sala ya no está donde estaba.

1. Seleccione **`Root/Planta_Piloto/Punto_Destino_Planta`** y apunte su posición.
2. Cree dos objetos vacíos más:

| Nombre | Position |
|---|---|
| `Marca_Reto2_A` | la de `Punto_Destino_Planta`, **restando 1 a la X** |
| `Marca_Reto2_B` | la de `Punto_Destino_Planta`, **sumando 1 a la X** |

### 1.3 Comprobación

Más adelante, cuando haya añadido el componente `Reunión en el centro`, al seleccionarlo verá
en la vista de escena un círculo dibujado sobre cada marca y una línea naranja entre ellas.
**Esa línea es la separación con la que los dos participantes llegarán al reto siguiente.** Si
la ve demasiado corta, separe más las marcas.

---

## Parte 2 · Avance del reto 1 al reto 2

Hoy funciona así: el estudiante acierta en el lavamanos → `FadeOutVR` espera 25 segundos →
funde a negro → **mueve solo a quien terminó** → enciende el lienzo del reto 2.

Ese «mueve solo a quien terminó» es el defecto que rompe la protección frente a choques.

### 2.1 Crear el objeto

Sobre `Root`: clic derecho → **Create Empty**. Nómbrelo **`Avance_Reto1`**.

### 2.2 Añadirle la acción de aviso

Con `Avance_Reto1` seleccionado, en el Inspector: **Add Component**, y en el buscador escriba
**`UnityEventAction`**.

> Estas acciones de VIROO **no están en ningún submenú**. No las busque en «Viroo → Actions»,
> porque ahí no aparecen: se generan al compilar y solo salen escribiendo su nombre en el
> buscador de Add Component.

**No toque todavía el campo `Exposed Event`. Déjelo vacío.** Ese campo sirve para llamar a un
método de algún componente de este objeto, y de momento no hay ninguno al que llamar. Si pulsa
el **+** ahora, le saldrá una fila en la que el desplegable `Function` solo ofrece `GameObject`
y `Transform`. Se rellena en el paso 2.6, cuando ya estén puestos los demás componentes.

Del resto de campos que verá:

| Campo | Qué poner | Por qué |
|---|---|---|
| `Menu Label` | **vacío** | Es el nombre con el que la acción aparecería en el menú de VIROO para dispararla a mano. Ésta la dispara el código |
| `Starts Enabled` | **marcado** | Como viene |
| `Persistence Type` | **None** | Con cualquier otro valor, VIROO recordaría que la acción ya se ejecutó y se la reproduciría a quien entrase tarde a la sesión. Con el reinicio entre tandas eso es justo lo contrario de lo que queremos |
| `Delay` | **0** | El retardo lo lleva `AvanceEnGrupo` |

### 2.3 Añadirle la acción de teletransporte

**Add Component** → escriba **`TeleportAllAction`**.

**Deje el campo `Teleport Position` VACÍO.** Lo rellena el código justo antes de ejecutarlo. Si
le pone algo, se sobrescribe igualmente.

### 2.4 Añadirle el avance en grupo

**Add Component** → **`ViroLab/Multiusuario/Avance en grupo`**.

Rellene así:

| Campo | Qué arrastrar o escribir |
|---|---|
| **Anuncio de fin** | arrastre el propio objeto `Avance_Reto1`; Unity coge su `UnityEventAction` |
| **Salto de grupo** | arrastre otra vez `Avance_Reto1`; Unity coge su `TeleportAllAction` |
| **Destino** | `Root/Planta_Piloto/Punto_Destino_Planta` |
| **Esperar a todos** | marcado |
| **Espera máxima** | 180 |
| **Margen antes del salto** | 2 |
| **Margen tras el salto** | 0.35 |
| **Log** | marcado, por ahora |

### 2.5 Añadirle la reunión

**Add Component** → **`ViroLab/Multiusuario/Reunión en el centro`**.

| Campo | Valor |
|---|---|
| **Marcas** | ponga `Size` a **2**, y arrastre `Marca_Reto1_A` al elemento 0 y `Marca_Reto1_B` al 1 |
| **Radio** | 0.8 |
| **Esperar A Que Termine** | arrastre **`Root/Planta_Piloto/Lavamanos/SND_Click_Lavamanos`** |
| **Espera Adicional** | 0 |
| **Espera Maxima Del Audio** | 30 |
| **Avance** | arrastre el propio objeto `Avance_Reto1`; Unity coge su `AvanceEnGrupo`. Si lo deja vacío, el componente lo busca solo en este mismo objeto, pero es mejor dejarlo a la vista |
| **Espera máxima** | 60 |
| **Log** | marcado |

> **Por qué hay que rellenar `Esperar A Que Termine`.** Al acertar en el lavamanos suena
> `FX_Reto3Botas.ogg`, que dura **19 segundos**, y la puerta tarda **5 segundos** en abrirse
> (`duracionApertura: 5` en `Bisagra_Puerta`). Los 25 segundos que esperaba el `FadeOutVR`
> antiguo cubrían justo eso. Si deja este campo vacío, se convoca a la marca de inmediato,
> cortando la locución y con la puerta a medio abrir.
>
> Se espera al audio, en lugar de volver a poner 25 segundos a mano, para que si algún día se
> cambia esa locución por una más corta o más larga la pausa siga siendo la correcta.

> El orden de las marcas importa poco, pero tiene que ser el mismo en todos los clientes, y lo
> es porque las dos partes leen el mismo array. El reparto va por orden de identificador de
> cliente.

### 2.6 Cerrar el circuito del aviso

> **Antes de seguir, compruebe que `Avance_Reto1` tiene los cuatro componentes**, en este
> orden o en cualquier otro:
>
> - `Transform`
> - `UnityEventAction`
> - `TeleportAllAction`
> - `Avance en grupo`
> - `Reunión en el centro`
>
> Si le falta alguno, el desplegable `Function` del paso siguiente no ofrecerá
> `RegistrarAnuncio` y parecerá que el campo está roto. No lo está: es que no hay a qué
> apuntar.

Esto es lo que hace que los dos visores se enteren de que alguien terminó.

En el componente **`UnityEventAction`** de `Avance_Reto1`, busque el campo **`Exposed
Event`** y pulse el **+**:

- **Object**: arrastre el propio objeto `Avance_Reto1`.
- **Function**: despliegue y elija **`AvanceEnGrupo → RegistrarAnuncio ()`**.

> Asegúrese de que el desplegable de la izquierda diga **Runtime Only**, no Editor And
> Runtime.

### 2.7 El fundido

Antes de cablearlo hay que crear el objeto que lo hace, porque `FundidoDePantalla` se crea solo
en tiempo de ejecución y por tanto no existe en la escena para poder arrastrarlo.

**Hágalo una sola vez, sirve para toda la experiencia:**

1. Sobre `Root`: **Create Empty**, nómbrelo **`Fundido`**.
2. **Add Component** → **`ViroLab/Fundido de pantalla`**.
3. Deje `Duracion` como venga.

Ahora, en el componente **`Avance en grupo`** de `Avance_Reto1`:

| Campo | Object | Function |
|---|---|---|
| **Antes del salto** | `Fundido` | `FundidoDePantalla → OscurecerAhora ()` |
| **Después del salto** | `Fundido` | `FundidoDePantalla → AclararAhora ()` |

> **El `Margen antes del salto` de 2 segundos del paso anterior es lo que hace que esto se vea
> bien.** El fundido tarda 2 segundos en llegar a negro; sin ese margen el teletransporte
> ocurriría en el mismo fotograma en que empieza a oscurecer, y el participante vería el salto
> a plena luz con el negro llegándole después.

> **Por qué `Después del salto` y no el evento del teletransporte.** Esto me obligó a añadir
> código después de escribir la primera versión de esta guía. El `TeleportAllAction` de VIROO
> **no expone ningún evento en el Inspector**: tiene un `OnActionExecuted`, pero es un evento
> de C#, no un `UnityEvent` serializado, así que no hay forma de engancharse a él desde el
> editor. Lo comprobé leyendo los metadatos del ensamblado. Por eso `AvanceEnGrupo` tiene su
> propio campo `Después del salto`, que sí se dispara, y en todos los clientes.

### 2.8 Mover lo que hoy enciende el reto 2

Hoy, cuando `FadeOutVR` termina su teletransporte, dispara cuatro cosas. **Hay que moverlas al
`Después del salto`**, porque el camino antiguo va a dejar de usarse.

Seleccione **`Root/Planta_Piloto/Lavamanos`** y mire el componente `FadeOutVR`, campo
**`On Teletransporte Ejecutado`**. Verá exactamente estas cuatro entradas:

| Object | Function | Argumento |
|---|---|---|
| `Canvas_Esc1` | `GameObject.SetActive` | **false** (desmarcado) |
| `Canvas _Esc2` | `GameObject.SetActive` | **true** (marcado) |
| `Bisagra_Puerta` | `ControladorPuerta.CerrarInstante` | — |
| `RoomTone_PlantaPiloto` | `AudioSource.Play` | — |

**Cópielas una por una** al campo **`Después del salto`** del `Avance en grupo` de
`Avance_Reto1`, con los mismos objetos, las mismas funciones y los mismos argumentos.

Quedarán cinco entradas, y **el orden importa**. Deje `AclararAhora` la última:

| # | Object | Function | Argumento |
|---|---|---|---|
| 1 | `Canvas_Esc1` | `GameObject.SetActive` | **false** |
| 2 | `Canvas _Esc2` | `GameObject.SetActive` | **true** |
| 3 | `Bisagra_Puerta` | `ControladorPuerta.CerrarInstante` | — |
| 4 | `RoomTone_PlantaPiloto` | `AudioSource.Play` | — |
| 5 | `Fundido` | `FundidoDePantalla.AclararAhora` | — |

Las entradas de un UnityEvent se ejecutan en el orden de la lista. Con `AclararAhora` al final,
el cambio de lienzo y el cierre de la puerta ya han ocurrido cuando la pantalla empieza a
aclarar, así que el participante no ve la puerta cerrarse de golpe delante de él.

**No borre todavía las originales.** Se quedan donde están hasta la parte 7.

### 2.9 Apuntar la salida del reto 1 al camino nuevo

Seleccione **`Root/Planta_Piloto/Lavamanos`** y mire el componente **`ValidadorSecuencia`**,
campo **`Al Tener Exito`**. Verá cinco entradas. La cuarta es:

- `Lavamanos` → `FadeOutVR.IniciarFadeOut`

**Cámbiela** (no la borre, edítela):

- **Object**: arrastre `Avance_Reto1`.
- **Function**: **`ReunionEnElCentro → Convocar ()`**.

Las otras cuatro entradas (el sonido del clic, el `SetActive`, el `set_enabled` y el
`AbrirPuerta`) **se quedan como están**.

> **Los 25 segundos del `FadeOutVR` antiguo.** Ya está resuelto: los sustituye la espera al
> audio que configuró en el paso 2.5, que es la misma pausa pero medida en vez de fijada a
> mano.

### 2.10 Comprobación de esta parte

Entre en **Play** en el editor (con un solo cliente vale) y haga el reto 1. En la consola debe
ver, en este orden:

```
[ReunionEnElCentro] Convocado al puesto 0: marca Marca_Reto1_A.
[AvanceEnGrupo] Anunciando fin de reto al resto de la sala.
[AvanceEnGrupo] Han terminado 1 de 1.
[AvanceEnGrupo] Ejecutando el salto de grupo (están todos).
```

Entre la primera línea y la segunda tiene que caminar hasta la marca. Si la segunda línea
aparece sola y de inmediato, revise que asignó las marcas en el paso 2.5.

---

## Parte 3 · Avance del reto 2 al reto 3

Misma estructura que la parte 2, con otros nombres. Va escrita entera para que no tenga que
volver atrás.

Lo que cambia respecto al reto 1: aquí la locución de cierre (`Cierre_Reto2.ogg`, 18 segundos)
la reproduce el propio cuestionario, no un objeto de la escena, así que el `AudioSource` al que
hay que esperar está en otro sitio. Y el objeto `Fundido` ya existe: no se crea otro.

### 3.1 Crear el objeto

Sobre `Root`: clic derecho → **Create Empty**. Nómbrelo **`Avance_Reto2`**.

### 3.2 Añadirle las dos acciones de VIROO

Con `Avance_Reto2` seleccionado, **Add Component** y escriba en el buscador:

1. **`UnityEventAction`** — dejar `Exposed Event` vacío por ahora, `Menu Label` vacío,
   `Persistence Type` en **None**, `Delay` en 0.
2. **`TeleportAllAction`** — dejar `Teleport Position` **vacío**.

### 3.3 Añadirle el avance en grupo

**Add Component** → **`ViroLab/Multiusuario/Avance en grupo`**.

| Campo | Valor |
|---|---|
| **Anuncio de fin** | arrastre el propio `Avance_Reto2`; Unity coge su `UnityEventAction` |
| **Salto de grupo** | arrastre otra vez `Avance_Reto2`; coge su `TeleportAllAction` |
| **Destino** | **`Root/Planta_Piloto/Punto_Inicio_Escena3`** |
| **Esperar a todos** | marcado |
| **Espera máxima** | 180 |
| **Margen antes del salto** | 2 |
| **Margen tras el salto** | 0.35 |
| **Log** | marcado |

> El `Margen antes del salto` puede bajarlo a **1** si deja el `Fundido` en 0,8 s. Tiene que ser
> mayor o igual que la duración del fundido; por debajo, el salto ocurre antes de que la
> pantalla esté negra.

### 3.4 Añadirle la reunión

**Add Component** → **`ViroLab/Multiusuario/Reunión en el centro`**.

| Campo | Valor |
|---|---|
| **Marcas** | `Size` = **2**. Elemento 0: `Marca_Reto2_A`. Elemento 1: `Marca_Reto2_B` |
| **Radio** | 0.8 |
| **Esperar A Que Termine** | el `AudioSource` de **`Root/Planta_Piloto/Canvas_Cuestionario`** |
| **Espera Adicional** | 0 |
| **Espera Maxima Del Audio** | 30 |
| **Caminar Mientras Habla** | **marcado** |
| **Avance** | arrastre `Avance_Reto2` |
| **Espera máxima** | 60 |
| **Log** | marcado |

> **Ojo con `Esperar A Que Termine`.** Aquí no es un objeto de sonido propio como en el reto 1:
> es el `AudioSource` que está en el mismo objeto que el cuestionario,
> `Root/Planta_Piloto/Canvas_Cuestionario`. Ese objeto arranca **apagado**, así que para verlo
> en la jerarquía hay que desplegar `Planta_Piloto`. Es el que reproduce `Cierre_Reto2.ogg`.

### 3.5 Cerrar el circuito del aviso

> Compruebe antes que `Avance_Reto2` tiene los cinco componentes: `Transform`,
> `UnityEventAction`, `TeleportAllAction`, `Avance en grupo` y `Reunión en el centro`. Si falta
> alguno, el desplegable del paso siguiente no ofrecerá lo que busca.

En el **`UnityEventAction`** de `Avance_Reto2`, campo **`Exposed Event`**, pulse **+**:

- **Object**: arrastre `Avance_Reto2`.
- **Function**: **`AvanceEnGrupo → RegistrarAnuncio ()`**.
- Desplegable izquierdo en **Runtime Only**.

### 3.6 El fundido y lo que se enciende al llegar

En el componente **`Avance en grupo`** de `Avance_Reto2`:

| Campo | Orden | Object | Function | Argumento |
|---|---|---|---|---|
| **Antes del salto** | — | `Fundido` | `FundidoDePantalla.OscurecerAhora` | — |
| **Después del salto** | 1 | `Canvas_Esc3` | `GameObject.SetActive` | **true** |
| **Después del salto** | 2 | `Fundido` | `FundidoDePantalla.AclararAhora` | — |

`AclararAhora` va la última, por lo mismo que en el reto 1: las entradas se ejecutan en orden y
conviene que el lienzo ya esté encendido cuando la pantalla empiece a aclarar.

La entrada de `Canvas_Esc3` la copia del `On Transition Complete` del `GestorCuestionario`,
donde está hoy. **No la borre de allí todavía**: eso es la parte 7.

### 3.7 Apuntar la salida del reto 2

Seleccione **`Root/Planta_Piloto/Canvas_Cuestionario`** y, en su componente
`GestorCuestionario`, rellene los dos campos:

| Campo | Valor |
|---|---|
| **Reunion** | arrastre `Avance_Reto2` |
| **Avance En Grupo** | arrastre `Avance_Reto2` |

Con `Reunion` relleno, al terminar el cuestionario se convoca de inmediato y el participante
camina hacia su marca mientras suena la locución de cierre; el salto espera a que termine.

Mientras los dos campos estén vacíos, el cuestionario sigue teletransportando por su cuenta
como hoy y avisa por consola cada vez.

### 3.8 Comprobación

Entre en **Play** y llegue hasta el final del cuestionario. La consola debe mostrar:

```
[ReunionEnElCentro] Convocado al puesto 0: marca Marca_Reto2_A.
[ReunionEnElCentro] Anunciando el fin (ha pisado su marca).
[AvanceEnGrupo] Anunciando fin de reto al resto de la sala.
[AvanceEnGrupo] Han terminado 1 de 1. En la sala: ...
[AvanceEnGrupo] Ejecutando el salto de grupo (están todos).
```

La segunda línea no debe aparecer hasta que la locución de cierre haya terminado, aunque usted
ya esté sobre la marca.

## Parte 4 · El reinicio entre tandas

Esta es la parte que decide si el día 29 sale. Hoy, para dejar la experiencia lista para la
pareja siguiente, hay que cerrar la aplicación y relanzarla desde el Room Player. Veinte veces.

### 4.1 Crear el objeto

Sobre `Root`: **Create Empty**, nómbrelo **`Reinicio_Experiencia`**.

### 4.2 Las dos acciones de VIROO

**Add Component** → **`UnityEventAction`**.
**Add Component** → **`TeleportAllAction`** (con `Teleport Position` vacío).

### 4.3 La foto de la escena

**Add Component** → **`ViroLab/Multiusuario/Foto de la escena`**.

| Campo | Valor |
|---|---|
| **Raíces** | `Size` = **2**. Elemento 0: arrastre **`Root`**. Elemento 1: arrastre **`_SimDashboard`** |
| **Incluir componentes** | **marcado** |
| **Log** | marcado |

**Por qué esto existe y por qué importa.** Inspeccionando la escena encontré tres componentes
que se desactivan a sí mismos cuando se usan: el lavamanos, el maniquí y el casillero
`locker room (3)` llaman a `set_enabled(false)` sobre su propio componente de interacción al
completarse. Sin devolverlos, **la segunda pareja del día entraría y no podría hacer clic en
nada**.

Podría haberle pedido que enumerara a mano esos tres componentes y los siete lienzos que
cambian de estado. Serían diez entradas que hay que mantener al día cada vez que se toque la
escena, y la que se olvide es un fallo que no aparece hasta la tanda siguiente. En vez de eso,
este componente fotografía el estado de todo al arrancar y lo devuelve entero.

**`_SimDashboard` está en la raíz de la escena, no dentro de `Root`.** Por eso hay que
arrastrarlo aparte. Si se le olvida, el tablero del simulador no se reiniciará.

### 4.4 El orquestador

**Add Component** → **`ViroLab/Multiusuario/Reinicio de la experiencia`**.

| Campo | Valor |
|---|---|
| **Aviso de reinicio** | el `UnityEventAction` de 4.2 |
| **Salto al inicio** | el `TeleportAllAction` de 4.2 |
| **Punto de inicio** | **`Root/RoomCenterStart`** |
| **Duración fundido** | 1.5 |
| **Margen tras el salto** | 0.5 |
| **Margen antirrebote** | 3 |
| **Al empezar el reinicio** | **déjelo vacío** |
| **Al terminar el reinicio** | **déjelo vacío** |
| **Log** | marcado |

Los dos eventos se quedan vacíos a propósito. De los lienzos y los componentes ya se encarga la
foto del paso anterior; están ahí solo por si algún día hace falta algo más.

### 4.5 Cerrar el circuito

En el **`UnityEventAction`** de `Reinicio_Experiencia`, campo **`Exposed Event`**, pulse **+**:

- **Object**: arrastre `Reinicio_Experiencia`.
- **Function**: **`ReinicioDeExperiencia → ReiniciarAqui ()`**.
- Desplegable izquierdo en **Runtime Only**.

---

## Parte 5 · El botón del moderador

El botón que usted va a pulsar veinte veces el día 29.

1. Póngalo **en el cliente de escritorio, fuera del alcance de los estudiantes**. El margen
   antirrebote evita el doble clic, pero no evita un clic ajeno.
2. En su `Button`, campo `On Click`, pulse **+**:
   - Object: `Reinicio_Experiencia`
   - Function: **`ReinicioDeExperiencia → PedirReinicio ()`**

> Hay otros dos botones de emergencia que conviene tener a mano el día de la prueba, por si una
> pareja se atasca:
>
> - **`AvanceEnGrupo → ForzarAvance ()`**: salta al reto siguiente sin esperar a nadie.
> - **`ReunionEnElCentro → ForzarAnuncio ()`**: da por buena la reunión sin que nadie pise su
>   marca.

---

## Parte 6 · La prueba, con dos visores

En este orden. Si algo falla, pare ahí y avíseme antes de seguir.

| # | Qué hacer | Qué tiene que pasar |
|---|---|---|
| 1 | Terminar el reto 1 | Aparece el aviso y **en cada visor se resalta una marca distinta** |
| 2 | Que los dos pisen su marca | Saltan **a la vez** y siguen a 2 m el uno del otro |
| 3 | Ya en el reto 2, que uno camine de espaldas hacia el otro | **Suena el aviso y aparece el anillo rojo** |
| 4 | En el simulador, operar deprisa | **Ninguna frase se corta a media palabra** |
| 5 | Pulsar el botón del moderador | Vuelven los dos al inicio: casilleros llenos, cuestionario sin empezar, simulador apagado, **y el lavamanos y el maniquí vuelven a responder al clic** |
| 6 | Repetir la experiencia entera sin cerrar la aplicación | Funciona igual que la primera vez |

**El punto 3 es el que decide si podemos pasar de dos estudiantes.** Es la única comprobación
que no puedo hacer yo: solo se resuelve en la sala.

**El punto 6, cronométrelo.** Ese número, dividiendo los 480 minutos útiles de la jornada, dice
cuántas tandas caben de verdad el día 29.

---

## Parte 7 · Lo que se retira, y solo después de la parte 6

**No haga nada de esto antes de haber pasado la prueba.** Mientras tanto, los caminos antiguos
son la red de seguridad.

| Qué | Dónde | Cómo |
|---|---|---|
| El teletransporte individual | `Lavamanos` → `FadeOutVR` | Borrar las cuatro entradas de `On Teletransporte Ejecutado` (ya están copiadas en el avance) |
| El teletransporte individual | `Canvas_Cuestionario` → `GestorCuestionario` | Borrar la entrada de `On Transition Complete` |
| Los destinos separados | `Root/Planta_Piloto/Punto_Inicio_Escena3/` hijos **`_B`**, **`_C`**, **`_D`** | Borrar los tres objetos |
| El componente entero | `Root/Ajuste_DestinosEscena3` | Borrar el objeto |

Los tres puntos `_B`, `_C` y `_D` son los destinos separados que repartían a cada participante
a un sitio distinto. Eso es exactamente lo contrario de lo que exige una sala compartida, y es
la causa de que el aviso de proximidad no funcione.

Cuando los haya borrado, dígamelo y limpio el código que los usaba.

---

## Anexo A · Mapa de la escena, tal como está hoy

Leído del archivo de escena, no de memoria.

| Objeto | Estado al arrancar | Qué es |
|---|---|---|
| `Root/RoomCenterStart` | activo | Centro de la sala física, en (-2.08, 0, -7.26) |
| `Root/Planta_Piloto/Canvas_Esc1` | **activo** | Lienzo del reto 1 |
| `Root/Planta_Piloto/Canvas _Esc2` | apagado | Lienzo del reto 2 (**ojo al espacio en el nombre**) |
| `Root/Planta_Piloto/Canvas_Esc3` | apagado | Lienzo del reto 3 |
| `Root/Planta_Piloto/Canvas_Cuestionario` | apagado | El cuestionario |
| `Root/Planta_Piloto/Canvas_VideoEsc3` | apagado | El vídeo del reto 3 |
| `Root/Planta_Piloto/Canvas_carrusel` | **activo** | El carrusel interactivo |
| `Root/Planta_Piloto/Lavamanos` | activo | `ValidadorSecuencia` + `FadeOutVR` |
| `Root/Planta_Piloto/Vitrina/Maniqui` | activo | `ValidadorSecuencia` |
| `Root/Planta_Piloto/Bisagra_Puerta` | activo | `ControladorPuerta` |
| `Root/Planta_Piloto/Manager_Escena3` | activo | `GestorEscena3` |
| `Root/Planta_Piloto/Punto_Destino_Planta` | activo | Destino del reto 1 |
| `Root/Planta_Piloto/Punto_Inicio_Escena3` | activo | Destino del reto 3 |
| `Root/Ajuste_DestinosEscena3` | activo | **A retirar** |
| `_SimDashboard` | activo | Tablero del simulador 2D, **en la raíz, no dentro de `Root`** |

---

## Anexo B · Si algo no sale

| Síntoma | Causa probable |
|---|---|
| «No hay acción de difusión asignada» en la consola | Falta el `UnityEventAction` en el campo `Anuncio de fin` o `Aviso de reinicio` |
| «Falta el TeleportAllAction» | Falta el componente en el objeto, o el campo `Salto de grupo` / `Salto al inicio` sin asignar. **Si sale este error, la difusión por red ya funcionó**: el aviso salió, viajó y volvió |
| «Avance individual: no hay AvanceEnGrupo asignado» | Falta rellenar el campo `avanceEnGrupo` de `FadeOutVR` o `GestorCuestionario`. **No es un error**: avisa de que todavía va por el camino antiguo |
| El grupo salta dos veces | Los dos clientes están ejecutando el teletransporte. Avíseme: es un fallo del reparto de turnos |
| Tras el reinicio no se puede hacer clic en el lavamanos | Falta la `Foto de la escena`, o le falta alguna raíz, o `Incluir componentes` está desmarcado |
| Tras el reinicio el tablero del simulador sigue encendido | Falta `_SimDashboard` en las raíces de la foto |
| El aviso de reunión no espera a nada | Faltan las marcas, o el `Radio` es enorme |
| «Falta el AvanceEnGrupo: el grupo no avanzará» | Está sin asignar el campo `Avance` de la `Reunión en el centro`, y tampoco hay un `AvanceEnGrupo` en ese objeto. **Si sale este error, lo demás funcionó**: la reunión convocó, asignó marca y detectó la llegada |
| En el editor la marca se da por pisada al instante | Normal. Sin visor, la cámara arranca cerca del centro de la sala, que es donde están las marcas. La prueba de verdad es con los visores |

Cualquier cosa que no encaje con esto, pásemela con la línea de consola y la miro.
