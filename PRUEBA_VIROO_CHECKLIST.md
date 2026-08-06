# Primera prueba en el sistema VIROO — lista de comprobaciones

Simulador de Pasteurización VR — UNAD

Todo lo verificable en modo Play ya está verificado. Esta lista cubre **solo lo que
únicamente puede comprobarse con visor**, ordenado de forma que si algo falla pronto,
no gastes la sesión en pruebas que dependen de ello.

---

## Antes de ir al laboratorio

- [ ] `Window → Viroo → Dashboard → Project Validation` en **cero errores**. Aplicar los
      *Fix* automáticos si aparecen.
- [ ] `Window → Viroo → Dashboard → Application Builder`:
      - **Application Identifier** (único de la organización) y **Application Name**
      - Registrar `Escena 1` y marcarla como **escena inicial / lobby**
      - No añadir escenas a mano en Build Settings salvo `VIROO Main` en el índice 0
- [ ] Generar el build y **subirlo a VIROO Cloud** (el `.zip` no se ejecuta directamente).
- [ ] Confirmar que el modo estéreo sigue en **Multi-Pass** (VIROO lo exige) y la calidad
      en **Ultra**. Ambos ya están así; solo verificar que el build no los cambió.

---

## Bloque 1 — Que arranque (si falla, nada más importa)

- [ ] La aplicación carga y el estudiante aparece en la **zona de alistamiento**,
      mirando hacia el tablero de la pared del fondo.
- [ ] Se reproduce el **video de introducción** y el splash de la escena 1.
- [ ] El estudiante ve sus **manos/mandos** y el rayo del puntero.
- [ ] Con 4 visores: los cuatro **se ven entre sí** como avatares y **se escuchan**.

---

## Bloque 2 — Interacción básica con el mando

- [ ] Apuntar y pulsar el gatillo sobre **Casillero**, **Maniquí** y **Lavamanos**:
      los tres responden.
- [ ] Al completar los tres, arranca la transición. **Ojo: hay 25 segundos de espera**
      configurados antes del fundido. Parece que no pasa nada; es normal.

---

## Bloque 3 — Transición a la planta

- [ ] Fundido a negro y teletransporte.
- [ ] Aparece el **splash de la escena 2**.
- [ ] La **puerta se cierra** a la espalda y arranca el **sonido ambiente** de la planta.

---

## Bloque 4 — Carrusel (cambios recientes)

- [ ] Los botones **ANTERIOR** y **SIGUIENTE** responden al rayo del mando.
- [ ] **No aparece ninguna ficha del pasteurizador** al pulsarlos.
      *Este es el punto crítico: la guarda de interfaz se probó con ratón, no con el
      rayo del mando. Si aquí salta la ficha, hay que cubrir también la vía XR.*
- [ ] El **rótulo "Carrusel interactivo"** se lee desde donde el estudiante se detiene
      físicamente, que puede ser más lejos que en el editor.

---

## Bloque 5 — Cuestionario (raycasters añadidos, sin probar en visor)

- [ ] Los botones **A, B y C responden al rayo**.
      *Sin esto el reto 2 es imposible de completar. Es el cambio del commit `5f0c64f`
      y nunca se ha probado con mando.*
- [ ] Respuesta incorrecta: se marca en rojo y permite reintentar.
- [ ] Respuesta correcta de la última pregunta: la **voz en off de cierre se escucha
      entera** (18 s) y el vídeo **no la pisa**.

---

## Bloque 6 — Escena 3

- [ ] Tras el fundido aparece el **splash de la escena 3**, revelado por el aclarado
      (no de golpe).
- [ ] Al terminar el splash arranca el **vídeo explicativo** del simulador.
- [ ] El **tablero SCADA de la pared** se lee desde donde el estudiante se sitúa
      físicamente. Anotar si el texto queda pequeño.

---

## Bloque 7 — Simulador

- [ ] Los botones del tablero responden al rayo.
- [ ] La secuencia con bloqueos funciona: Energía → Iniciar → Llenado → Calefactor →
      Bomba de producto.
- [ ] **Reiniciar Lote** devuelve todo al estado inicial: energía apagada, tanques y
      temperaturas a cero, manómetro y pulmón a cero, y reaparece la ventana EMPEZAR.

---

## Bloque 8 — Explorador de piezas (el cambio sin verificar)

- [ ] Apuntar con el mando a una pieza del pasteurizador y **pulsar el gatillo**:
      se resalta y aparece su ficha con el tag y la descripción correctos.

**Si no responde nada**, el enlace automático con el rig no encontró el interactor.
Plan B sin recompilar: añadir a la escena un `ControllerButtonPressInteraction` de
VIROO (controlador derecho, botón Trigger) y cablear su evento al método
**`NotifyExternalClick`** del componente `PasteurizerHoverHandler`, que ya existe
precisamente para esto.

---

## Bloque 9 — Confort y desplazamiento físico

- [ ] Caminando por la sala, la **correspondencia entre el espacio físico y el virtual**
      es correcta (no se atraviesan paredes reales ni se choca con aire).
- [ ] La **ficha de descripción** flota a 85 cm de la cara. Comprobar si estorba al
      caminar o al mirar la máquina.
- [ ] Nadie refiere mareo ni molestia. La ficha del proyecto exige poder **ajustar la
      intensidad** de la experiencia; anotar qué habría que poder regular.
- [ ] Anotar **fluidez** (si hay tirones) en las zonas con el pasteurizador a la vista:
      es el modelo más pesado, con 860 piezas.

---

## Notas para interpretar los resultados

- Los `_CollisionWalls` del pasteurizador están mal orientados, pero con **movimiento
  físico** los colisionadores virtuales no frenan al estudiante, así que no deberían
  notarse. Si se percibiera algún bloqueo, avisar.
- Los dos canvas informativos de los logos 3D no tienen raycaster XR **a propósito**:
  no llevan botones.
- El explorador de piezas y la selección son **locales por estudiante**: cada uno ve su
  propia ficha, y eso es lo decidido, no un fallo.
