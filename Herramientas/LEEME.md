# Auditor del cableado

Comprueba que la Escena 1 esta cableada como el codigo espera. Lee el archivo de
escena y los `.meta` del proyecto; no necesita Unity abierto ni entrar en Play.

```bash
python Herramientas/auditar_cableado.py
```

Cada comprobacion sale como **OK**, **AVISO** o **FALLO**.

- **FALLO**: algo que impedira que la experiencia funcione.
- **AVISO**: algo que conviene revisar, o un resto del camino antiguo que sigue
  ahi a proposito como red de seguridad.

Que mira, en siete bloques: el fundido, los dos avances en grupo con sus
acciones de VIROO y el orden de sus eventos, las reuniones con sus marcas y su
separacion, el reinicio entre tandas con la foto de la escena, las salidas de
los retos, los restos del camino antiguo, y las cadenas de llamadas que tienen
que seguir conectadas.

Paselo despues de tocar la escena y antes de compilar para el laboratorio.
