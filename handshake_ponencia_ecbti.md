# Handshake · Ponencia II Encuentro de Grupos y Semilleros ECBTI

Status: draft, interview in progress

## La idea en palabras sencillas

TODO · se escribe cuando pase la prueba de lectura de vuelta.

## Por qué importa

La ECBTI invitó al proyecto a presentarse en el lanzamiento del II Encuentro de
Grupos y Semilleros de Investigación, dentro del Foro "El Intersticio del Ethos:
ética y pensamiento crítico en la era de la inteligencia artificial", organizado
con el grupo AequitasLab.

No es una exposición técnica. Los organizadores piden explícitamente que el
proyecto sirva para "reflexionar sobre los alcances, las limitaciones y las
responsabilidades éticas" de incorporar tecnologías inmersivas en la formación.
Dos de los diez minutos van a eso.

## Para quién es

Comunidad académica de la ECBTI reunida en el auditorio de la Sede Nacional José
Celestino Mutis, más quien siga la transmisión en vivo. Investigadores y
semilleristas, no estudiantes de Ingeniería de Alimentos.

Consecuencia para el diseño: el público no conoce el proceso de pasteurización.
Lo que le interesa es el problema formativo, la decisión metodológica y la
tensión ética, no el detalle del intercambiador de placas.

## Qué existe hoy

Hechos verificados. Entre paréntesis, la fuente.

**Datos del proyecto** (Ficha_de_proyecto_UNAD-PIZCBC292025.pdf, Drive)

| Campo | Valor |
|---|---|
| Código | PIZCBC292025 |
| Tipo | Proyecto de Desarrollo Tecnológico (PDT) |
| Convocatoria | N.º 13 de 2024, "Servir al territorio en pro de soluciones solidarias e innovadoras". Estado: aprobado pares definitivo |
| Vigencia original | 03/02/2025 a 08/06/2026, 16 meses |
| Prórroga | Solicitada el 03/02/2026, seis meses más. Nueva fecha de cierre: 08/12/2026. TODO · confirmar que el Comité la aprobó |
| Equipo | Luis Montañez Carrillo, investigador principal · Clara Sánchez, coinvestigadora · Cesar David Monroy, coinvestigador |
| Valor | $126.358.953 |
| Unidad responsable | ECBTI. Avalan: Centro José Acevedo y Gómez, Tecnología en Producción de Audio, ZCBOG |
| Líneas | Ingeniería de Procesos en Alimentos y Biomateriales · Ingeniería de Software |
| Grupos | BYTE IN DESIGN · Etno-farmacología, Productos Naturales y Alimentos |
| Semilleros | ARCADE · Semillero de Investigación VITAL ZCBOY |
| ODS | 4, Educación de calidad |
| Aliado externo | Universidad de La Salle |

**Objetivo general** (misma fuente)

Desarrollar un prototipo de simulador de Realidad Virtual que facilite el
aprendizaje y la comprensión del proceso de pasteurización de alimentos
líquidos, mejorando la formación práctica de los estudiantes del programa de
Ingeniería de Alimentos en términos de seguridad alimentaria.

**Cifras del problema** (misma fuente)

- La UNAD es la única IES del país que ofrece Ingeniería de Alimentos con
  metodología virtual. Dieciocho universidades lo ofrecen de forma presencial
  (ACOFI, 2019).
- Instalar un laboratorio de pasteurización cuesta entre 250.000 y 1.000.000 de
  dólares (NMPF, 2022).
- Las enfermedades transmitidas por alimentos afectan a 600 millones de personas
  al año (OMS, 2020).
- Muestra prevista para la validación: 45 participantes.

**El prototipo** (Guion del simulador VR versión 2, Drive · repositorio VirooStudio)

Tres retos encadenados:

1. Zona de alistamiento. Bioseguridad según Resolución 2674 de 2013 del INVIMA:
   guardar objetos personales, equipar dotación, higienizar manos y botas.
2. Reconocimiento del proceso. Carrusel de tres láminas (fundamentos, tipos
   HTST/LTLT/UHT, flujo de planta) y cuestionario de cinco preguntas.
3. Simulación. Arranque del equipo, observación del ciclo térmico y pregunta de
   cierre sobre la válvula de desviación.

Tecnología: Unity 6000.0.49f1 sobre VIROO Studio 3.0.606. Sala física de
6,95 × 13,10 m con mapeo 1:1, hasta cuatro visores simultáneos más un
participante de escritorio, desplazamiento caminando de verdad.

**Discrepancia conocida que hay que resolver antes de publicar cifras.** El guion
describe tres escenas de Unity encadenadas con carga entre ellas. La
implementación real consolidó todo en una sola escena. El guion es el diseño, no
el estado del software.

**La validación se hizo en dos rondas, y el orden importa para el relato**

Ronda 1, **docentes**, septiembre de 2026. **Once entrevistas**
semiestructuradas de ocho preguntas, sesiones de unos 25 a 30 minutos en el
simulador. Siete docentes de la cadena de Alimentos y cuatro de Multimedia y
Audio. Cinco tienen el instrumento diligenciado y transcripción; las otras seis
tienen transcripción literal.

| Participante | Cursos que orienta | Cadena |
|---|---|---|
| E-01 | Termodinámica | Alimentos |
| E-02 | Termodinámica y Fenómenos de Transporte | Alimentos |
| E-03 | Toxicología y Alérgenos; Desarrollo e Innovación de Productos; Empaques; Microbiología | Alimentos |
| E-04 | Mecánica de Sólidos y Fluidos; Alimentación y Nutrición | Alimentos |
| E-05 | Producción y Posproducción de Audio; Audio Dinámico para Videojuegos | Multimedia |
| Yuneidys Oñate Perpiñán | Ingeniería de Alimentos y Tecnología. **Dicta el componente práctico** | Alimentos |
| Ruth Mary Benavides | Alimentos | Alimentos |
| Harvey Andrés Milquez | Ingeniería de Alimentos | Alimentos |
| Walter Fernández | Ingeniería Multimedia | Multimedia |
| Gustavo Bejarano | Ingeniería Multimedia | Multimedia |
| Cristian Orlando Perdomo Romero | Tecnología en Producción de Audio | Audio |

Las sesiones documentadas con instrumento son del 15 de septiembre de 2026;
las transcripciones se exportaron el 17. TODO · confirmar la fecha de sesión de
las seis entrevistas que solo tienen transcripción.

Ronda 2, **estudiantes**, 29 de septiembre de 2026. Instrumento 1, encuesta
previa, e Instrumento 2, encuesta posterior, ambos en Google Sheets en la misma
carpeta. En una presentación anterior aparecieron cifras de aprendizaje (5 a 9
y 7 a 10) que no coinciden con lo que arrojan los datos (4 a 8 y 6 a 10).
TODO · recontar desde los instrumentos antes de poner ningún número en una
lámina, y confirmar cuántos estudiantes participaron realmente.

**Evidencia textual que sostiene la postura ética** (entrevistas a docentes)

Primera experiencia y preparación:

- E-04: "Es la primera vez que el estudiante va a tener el equipo enfrente...
  la primera interacción del estudiante con algo que, a pesar de ser simulado,
  también representa lo real." Añade que el estudiante ve las dimensiones del
  equipo y la escala de una operación de 10 a 15 litros, frente a una
  definición o una imagen, "que resultan muy planas".
- E-01: "familiariza al estudiante con las condiciones iniciales de operación"
  de modo que "llegue con más confianza y una mejor experiencia al momento de
  trabajar en una planta piloto real".
- E-03, sobre la zona de alistamiento: "sin ello no se permitía la entrada, lo
  cual corresponde a una de las realidades que se viven en una planta de
  alimentos". Valida que arrancar por bioseguridad es pedagógicamente correcto.
- E-02: "afianza más los conceptos vistos en el curso". Señala que podría
  servir de componente práctico en Fenómenos de Transporte, que hoy usa
  VirtualPlant, y encuentra este desarrollo "más interactivo y atractivo".

Reducción de riesgos:

- E-03: "En la práctica con un pasteurizador real los estudiantes siempre
  tienen miedo de mover válvulas cuando se trabaja con presión y temperatura;
  esta herramienta les permitiría experimentar sin ese temor ni esa limitación."
- E-01: el estudiante "puede jugar con las condiciones de proceso, cometer
  errores y reiniciar el sistema sin que se produzcan pérdidas de lotes de
  producto ni de materia prima", lo que le permite "construir confianza
  profesional para la manipulación posterior del equipo real".

Acceso, como argumento secundario:

- E-05: "sobre todo por el hecho de estar a la distancia: seguramente hay
  estudiantes que, dependiendo de la región donde se encuentran, no pueden
  acceder a un componente práctico en una sede o centro".

**Fidelidad del modelo, cifrada por un experto del dominio**

- E-04 estima que el simulador describe alrededor del **80 %** de lo que ocurre
  en una planta real de pasteurización de leche. Nombra el 20 % que falta:
  conocer las características previas de la leche, y hacer más visible el
  tiempo de retención, "por ser el aspecto de mayor relevancia". Corrige además
  una denominación: lo llamado "calefactor" debería ser "intercambiador".
- E-03: "todo el proceso representado es fiel".
- E-01 matiza: el proceso está bien representado "pero solo para quien ya tiene
  conocimiento previo de los equipos". Un estudiante nuevo "se sentiría
  perdido".

**Dos hallazgos que aparecieron al leer las once y que cambian el relato**

1. **La planta piloto física no tiene este equipo.** Dos docentes de Alimentos
   lo dicen por separado, sin que se les pregunte:

   - Yuneidys Oñate, que dicta el componente práctico: "nosotros en planta
     piloto no tenemos un equipo así, hay un pasteurizador como un tanquecito
     que se conecta a la energía, pero no está así con el intercambiador de
     calor".
   - Ruth Mary Benavides: "en planta nosotros no tenemos el equipo... para un
     estudiante puede ser bastante interesante realmente ver un intercambiador
     de placas porque en planta no lo tenemos". Y añade: "a la planta le faltan
     muchos equipos".

   Consecuencia para el argumento: para el intercambiador de placas, el
   simulador no es solo la antesala del laboratorio físico. Es el único sitio
   donde ese equipo existe para el estudiante.

   **DECISIÓN: este hallazgo NO se menciona en la ponencia.** Decirlo en la
   Sede Nacional y con transmisión en vivo podría leerse como un reclamo de
   dotación a la institución que invita. Queda registrado aquí porque es
   relevante para el informe final del proyecto y para el artículo, no para
   esta charla.

2. **Accesibilidad, que es una cuestión ética y no solo de usabilidad.** Tres
   señales convergentes:

   - Harvey Milquez: "tuve un par de estudiantes sordos que me decían yo no
     puedo ir... me queda muy lejos ir al componente práctico. Entonces este
     tipo de cosas sería muy interesante que ellos la tuvieran, pero ¿cómo
     acercarlo?". El simulador depende hoy de voz en off.
   - Gustavo Bejarano, con astigmatismo: "para mí, que yo no tengo buena vista,
     fue muy útil el sonido". Es decir, el canal sonoro fue su vía principal.
     A la vez reporta que los textos son difíciles de leer.
   - Walter Fernández propone la solución: "yo redundaría con un texto, que se
     hable y también se pueda leer".

   Los dos canales tienen que ser redundantes, porque cada uno es la única vía
   de acceso para alguien.

**Limitaciones encontradas en la validación, para reportar con honestidad**

- El sonido no se escuchó. E-01, E-02, E-03 y E-04 no lograron identificar los
  sonidos de la maquinaria, por volumen demasiado bajo o por ruido cruzado
  entre participantes simultáneos. Walter Fernández diagnostica la causa con
  precisión: "los niveles estaban casi iguales, entonces se perdió". E-05
  critica el procesamiento y la espacialización de la voz. Coincide con el ítem
  peor valorado por los estudiantes.
- **Errores de contenido detectados por expertos del dominio.** Ruth Benavides
  encontró dos afirmaciones del cuestionario que no son del todo válidas: que
  la pasteurización elimina totalmente los microorganismos (hay esporas) y que
  no altera los nutrientes (las vitaminas sí se afectan). Yuneidys Oñate
  encontró que la infografía de bioseguridad muestra a una persona con arete
  mientras el texto exige retirar los accesorios.
- Legibilidad: textos pequeños y, según Gustavo Bejarano, "letras que
  bailaban", posiblemente un asunto de render en tiempo real. Harvey Milquez,
  que usa lentes, tuvo dificultad para leer y advierte que una práctica más
  larga sería cansada.
- Desorientación al empezar y al cambiar de reto. La piden explícitamente
  E-01, Gustavo Bejarano, Cristian Perdomo, Walter Fernández y Yuneidys Oñate.
  Cinco de once. Es el hallazgo más repetido de toda la validación.
- E-02: la transición del reto 2 al reto 3 no da señal clara de avance, e
  instrucciones duplicadas entre la ventana emergente y la voz del tablero.
- E-03: la sesión se cerró antes de alcanzar los 72 °C, posible límite de
  tiempo o batería baja del visor; imagen borrosa; las ventanas de información
  de los equipos no se le mostraron.
- E-04: comenzó la experiencia en un punto distinto al previsto y se topó con
  paredes físicas de la sala.
- E-01 pidió expresamente "un marcador de arranque que resalte la ruta a
  seguir". Es exactamente el indicador de pasos que se implementó el 29 de
  octubre de 2026.

**Insumos audiovisuales ya grabados** (29 de octubre de 2026, carpeta
`C:\Users\MSI\Videos\Captures`)

| Video | Duración |
|---|---|
| Reto 1, Zona de alistamiento | 1:56 |
| Reto 2, Reconocimiento del proceso | 2:56 |
| Reto 3, Simulación del proceso | 2:26 |
| **Total** | **7:19** |

Restricción dura: el bloque del prototipo son 3 minutos. Los videos completos no
caben, ni de lejos. Hay que recortar, montar o sustituir por capturas.

**Plantilla institucional**: `plantilla_presentaciones_sede_nacional.pptx`, 4,6 MB.
TODO · revisar su diagramación, paleta y familia tipográfica.

## Qué significa que salió bien

TODO

## Decisiones ya tomadas

- Entregable inmediato: un archivo Markdown de máximo 10 láminas, que después se
  mejora visualmente en Cowork.
- Mucha imagen, poco texto.
- Lenguaje académico, propio de un evento de investigación.
- Se respeta la identidad gráfica de la UNAD, sin alterar la plantilla.
- Se sigue la estructura de seis partes que pidieron los organizadores.
- **La postura ética: el simulador como antesala, no como sustituto.** Dos
  afirmaciones, y las dos salieron de las pruebas, no de una tesis previa.

  1. Es la **primera experiencia de entrar a una planta piloto**. Por eso la
     experiencia arranca en la zona de alistamiento y no en el equipo: el
     protocolo de bioseguridad es parte de lo que hay que aprender. El
     estudiante llega mejor preparado a su componente práctico presencial.
  2. Es la **primera experiencia con el proceso y con la máquina**. Llega con un
     conocimiento básico de cómo funciona un pasteurizador industrial, lo que
     reduce dos riesgos: el físico, de accidentarse al manipular el equipo real,
     y el del propio equipo, de dañarlo por desconocimiento.

  Lo importante para el relato: **estos enfoques emergieron de la validación**,
  en las entrevistas a docentes del 15 de septiembre y en las pruebas con
  estudiantes del 29 de septiembre. No se partió de ellos.

- **El límite que se reconoce en voz alta: el 80 %.** Un docente de la cadena
  cifró la fidelidad del modelo en ese porcentaje y nombró lo que falta. Ese
  dato se presenta, no se esconde: un modelo que no declara lo que recorta no
  es un modelo honesto. Responde directamente a lo que pidieron los
  organizadores sobre "la responsabilidad de representar adecuadamente la
  realidad mediante modelos tecnológicos".

  No se usan los otros dos límites que se consideraron: ni el riesgo de formar
  confianza sin competencia, ni el riesgo de que el simulador sirva de excusa
  institucional para no invertir en laboratorios.

- **Segundo límite: la accesibilidad.** La experiencia depende hoy de la voz en
  off, y un docente reportó estudiantes sordos que justamente no pueden
  desplazarse al componente práctico presencial. A la vez, un docente con
  astigmatismo dijo que el sonido fue su canal principal porque los textos no
  se leían. Cada canal es la única vía de acceso para alguien, de modo que los
  dos tienen que ser redundantes. Responde a "la equidad en el acceso para
  estudiantes de distintas regiones" que nombraron los organizadores, y la
  aterriza en un caso concreto en lugar de una generalidad.
- **El prototipo se muestra con tres clips de unos 50 segundos**, uno por reto,
  uno por lámina. Solo recorte, no edición. Los tres videos completos (7:19) se
  envían aparte a los organizadores, que los pidieron como anexo.
- **Los resultados se muestran con sus sombras**: lo que midió bien, lo que
  falló (se perdían, el sonido fue el ítem peor valorado) y qué se corrigió a
  partir de ahí. Es lo coherente con la postura ética elegida.
- **Próximos pasos**: el simulador como plataforma reutilizable para otros
  procesos; una línea conjunta sobre diseño de RV educativa para perfiles con
  poca experiencia digital; y llevar el prototipo a los CEAD como recurso
  institucional, enunciado como aspiración porque depende de decisiones que el
  proyecto no controla.

## Decisiones aún abiertas

1. **Confirmar que el Comité aprobó la prórroga.** Dato que va en la lámina de
   contexto. Por defecto se asume aprobada, porque las pruebas con estudiantes
   se hicieron el 29 de septiembre de 2026, posterior a la fecha de cierre
   original.
2. **Quién presenta.** Por defecto, Luis Montañez Carrillo, investigador
   principal. Los organizadores lo piden por escrito.
3. **Si se menciona o no el cierre de la validación con los 45 participantes
   comprometidos.** El autor no lo escogió entre los próximos pasos. Por defecto
   no se anuncia, pero la lámina de resultados sí dirá con cuántos estudiantes
   se probó realmente.

## Restricciones y líneas rojas

- Diez minutos exactos. Martes 3 de noviembre de 2026, 9:50 a.m.
- Máximo 10 láminas.
- Quedan cinco días.
- Ningún dato inventado. Toda cifra que aparezca debe estar verificada contra su
  fuente, y si no cuadra se reporta en vez de ajustarla en silencio.
- La identidad gráfica de la Universidad no se toca.

## Fuera de alcance

- El archivo .pptx final. El entregable de esta fase es el .md.
- Edición de video.

## Preguntas abiertas para investigación

1. ¿Qué cifras exactas arrojan los dos instrumentos de las pruebas del 29 de
   septiembre? Importa porque una cifra errónea en un evento transmitido en vivo
   es difícil de recoger.
2. ¿Qué diagramación, paleta y tipografía impone la plantilla institucional?
   Importa para que Cowork trabaje dentro de la identidad y no contra ella.
3. ¿Cuánto duran los tres videos? Importa para saber si caben en tres minutos.

## Notas para el armado

- La estructura de seis partes de los organizadores no se corresponde una a una
  con diez láminas. Hay que decidir el reparto.
- El público no es de Ingeniería de Alimentos. La lámina del proceso técnico
  debe explicarse sola.
