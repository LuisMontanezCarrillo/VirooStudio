# -*- coding: utf-8 -*-
"""Auditoria del cableado de la Escena 1 contra lo que espera el codigo.

No adivina: lee el archivo de escena y los .meta del proyecto, y comprueba una
por una las condiciones que tienen que cumplirse para que la experiencia
funcione. Cada linea sale como OK, AVISO o FALLO.
"""
import re, io, os, glob, math, sys

RAIZ = "C:/Users/MSI/Desktop/VirooStudio"
ESCENA = os.path.join(RAIZ, "Assets/Scenes/Escena 1.unity")

# ---------------------------------------------------------------- lectura
txt = io.open(ESCENA, encoding="utf-8", errors="replace").read()
trozos = re.split(r'^--- !u!(\d+) &(\d+).*$', txt, flags=re.M)
objs = {}
i = 1
while i + 2 <= len(trozos) - 1:
    objs[trozos[i + 1]] = (trozos[i], trozos[i + 2])
    i += 3

nom, act, go_de, tr_de, padre, pos = {}, {}, {}, {}, {}, {}
for fid, (cls, body) in objs.items():
    if cls == "1":
        m = re.search(r'm_Name: (.*)', body)
        nom[fid] = m.group(1).strip() if m else "?"
        a = re.search(r'm_IsActive: (\d)', body)
        act[fid] = a.group(1) if a else "?"
        for c in re.finditer(r'component: \{fileID: (\d+)\}', body):
            go_de[c.group(1)] = fid
    if cls in ("4", "224"):
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', body)
        if g: tr_de[g.group(1)] = fid
        f = re.search(r'm_Father: \{fileID: (\d+)\}', body)
        if f and f.group(1) != "0": padre[fid] = f.group(1)
        p = re.search(r'm_LocalPosition: \{x: ([-\d.eE+]+), y: ([-\d.eE+]+), z: ([-\d.eE+]+)\}', body)
        if p: pos[fid] = tuple(float(x) for x in p.groups())

def ruta(gofid):
    out, tr, v = [], tr_de.get(gofid), set()
    while tr and tr not in v:
        v.add(tr)
        g = re.search(r'm_GameObject: \{fileID: (\d+)\}', objs[tr][1])
        if not g: break
        out.append(nom.get(g.group(1), "?"))
        tr = padre.get(tr)
    return "/".join(reversed(out))

def mundo(gofid):
    tr, acc, v = tr_de.get(gofid), [0.0, 0.0, 0.0], set()
    while tr and tr not in v:
        v.add(tr)
        p = pos.get(tr, (0, 0, 0))
        acc = [acc[0] + p[0], acc[1] + p[1], acc[2] + p[2]]
        tr = padre.get(tr)
    return acc

GUID = {}
for meta in glob.glob(os.path.join(RAIZ, "Assets/**/*.cs.meta"), recursive=True):
    try:
        g = re.search(r'guid: (\w+)', io.open(meta, encoding="utf-8", errors="replace").read()).group(1)
    except Exception:
        continue
    GUID[os.path.basename(meta)[:-8]] = g

def componentes(nombre):
    """Todos los MonoBehaviour de un script dado: [(fid, cuerpo, ruta)]."""
    g = GUID.get(nombre)
    if not g: return []
    out = []
    for fid, (cls, body) in objs.items():
        if cls != "114" or ("guid: " + g) not in body: continue
        out.append((fid, body, ruta(go_de.get(fid, ""))))
    return sorted(out, key=lambda x: x[2])

def campo(body, nombre):
    m = re.search(r'^\s{2}' + nombre + r': \{fileID: (-?\d+)', body, re.M)
    if m: return m.group(1)
    m = re.search(r'^\s{2}' + nombre + r': (.+)$', body, re.M)
    return m.group(1).strip() if m else None

def lista(body, nombre):
    m = re.search(r'^\s{2}' + nombre + r':\s*\n((?:\s{2}- .*\n)*)', body, re.M)
    if not m: return []
    return re.findall(r'\{fileID: (-?\d+)\}', m.group(1))

def llamadas(body, evento):
    m = re.search(evento + r':\s*\n\s*m_PersistentCalls:\s*\n\s*m_Calls:\s*\n((?:\s+-.*\n(?:\s{6,}.*\n)*)*)', body)
    if not m: return []
    return re.findall(r'm_Target: \{fileID: (\d+)\}.*?m_MethodName: (\S+)', m.group(1), re.S)

def nombre_de(fid):
    if fid in (None, "0"): return "(vacío)"
    if fid in objs and objs[fid][0] == "1": return nom.get(fid, "?")
    go = go_de.get(fid)
    return ruta(go) if go else f"fileID {fid}"

# ---------------------------------------------------------------- informe
fallos, avisos = [], []
def linea(estado, texto):
    print(f"  [{estado:5s}] {texto}")
    if estado == "FALLO": fallos.append(texto)
    if estado == "AVISO": avisos.append(texto)

def comprobar(cond, texto, grave=True):
    linea("OK" if cond else ("FALLO" if grave else "AVISO"), texto)

print("=" * 78)
print("AUDITORIA DEL CABLEADO · Escena 1")
print("=" * 78)

# ---- 1. fundido
print("\n1. FUNDIDO DE PANTALLA")
fund = componentes("FundidoDePantalla")
comprobar(len(fund) == 1, f"hay exactamente un FundidoDePantalla en la escena (hay {len(fund)})")
dur_fundido = 0.8
if fund:
    d = campo(fund[0][1], "duracion")
    dur_fundido = float(d) if d else 2.0
    print(f"         objeto: {fund[0][2]}   duracion: {dur_fundido} s")

# ---- 2. avances en grupo
print("\n2. AVANCES EN GRUPO")
avances = componentes("AvanceEnGrupo")
comprobar(len(avances) >= 2, f"hay al menos dos avances en grupo (hay {len(avances)})")
for fid, body, r in avances:
    print(f"\n   --- {r} ---")
    an = campo(body, "anuncioDeFin"); sa = campo(body, "saltoDeGrupo"); de = campo(body, "destino")
    comprobar(an not in (None, "0"), f"tiene accion de aviso asignada")
    comprobar(sa not in (None, "0"), f"tiene TeleportAllAction asignado")
    comprobar(de not in (None, "0"), f"tiene destino asignado -> {nombre_de(de)}")
    mant = campo(body, "margenAntesDelSalto")
    if mant:
        comprobar(float(mant) >= dur_fundido,
                  f"el margen antes del salto ({mant} s) cubre el fundido ({dur_fundido} s)")
    antes = llamadas(body, "AntesDelSalto")
    desp = llamadas(body, "DespuesDelSalto")
    comprobar(any(m == "OscurecerAhora" for _, m in antes), "el fundido oscurece antes del salto", grave=False)
    comprobar(any(m == "AclararAhora" for _, m in desp), "el fundido aclara despues del salto", grave=False)
    if desp:
        ultimo = desp[-1][1]
        comprobar(ultimo == "AclararAhora",
                  f"AclararAhora es la ULTIMA entrada de «despues del salto» (es «{ultimo}»)", grave=False)
    # la accion de aviso tiene que llamar a RegistrarAnuncio de ESTE componente
    if an not in (None, "0") and an in objs:
        ll = llamadas(objs[an][1], "exposedEvent")
        ok = any(m == "RegistrarAnuncio" for _, m in ll)
        comprobar(ok, "su UnityEventAction llama a RegistrarAnuncio")
        pt = campo(objs[an][1], "persistenceType")
        comprobar(pt in (None, "0"), f"su UnityEventAction NO persiste (persistenceType={pt})")

# ---- 3. reuniones
print("\n3. REUNIONES EN EL CENTRO")
reuniones = componentes("ReunionEnElCentro")
for fid, body, r in reuniones:
    print(f"\n   --- {r} ---")
    marcas = [m for m in lista(body, "marcas")]
    comprobar(len(marcas) >= 2, f"tiene al menos dos marcas (tiene {len(marcas)})")
    comprobar(all(m != "0" for m in marcas), "ninguna marca esta vacia")
    if len(marcas) >= 2 and all(m != "0" for m in marcas):
        a, b = mundo(go_de.get(marcas[0], "")), mundo(go_de.get(marcas[1], ""))
        sep = math.hypot(a[0] - b[0], a[2] - b[2])
        comprobar(sep >= 1.5, f"las marcas estan separadas {sep:.2f} m (minimo recomendado 1,5)", grave=False)
    comprobar(campo(body, "avance") not in (None, "0"), "tiene el AvanceEnGrupo asignado")
    esp = campo(body, "esperarAQueTermine")
    comprobar(esp not in (None, "0"), f"espera a un audio de cierre -> {nombre_de(esp)}", grave=False)
    mh = campo(body, "mirarHacia")
    comprobar(mh not in (None, "0"), f"exige orientacion -> {nombre_de(mh)}", grave=False)
    cmh = campo(body, "caminarMientrasHabla")
    comprobar(cmh == "1", "camina mientras habla (ahorra el tiempo de la locucion)", grave=False)
    em = campo(body, "esperaMaxima")
    if em: comprobar(float(em) <= 90, f"la espera maxima ({em} s) no deja la sesion colgada mucho tiempo", grave=False)

# ---- 4. reinicio
print("\n4. REINICIO ENTRE TANDAS")
rein = componentes("ReinicioDeExperiencia")
comprobar(len(rein) == 1, f"hay exactamente un ReinicioDeExperiencia (hay {len(rein)})")
for fid, body, r in rein:
    print(f"   objeto: {r}")
    av = campo(body, "avisoDeReinicio")
    comprobar(av not in (None, "0"), "tiene accion de difusion asignada")
    comprobar(campo(body, "saltoAlInicio") not in (None, "0"), "tiene TeleportAllAction asignado")
    pi = campo(body, "puntoDeInicio")
    comprobar(pi not in (None, "0"), f"tiene punto de inicio -> {nombre_de(pi)}")
    if av not in (None, "0") and av in objs:
        ll = llamadas(objs[av][1], "exposedEvent")
        comprobar(any(m == "ReiniciarAqui" for _, m in ll), "su UnityEventAction llama a ReiniciarAqui")
        pt = campo(objs[av][1], "persistenceType")
        comprobar(pt in (None, "0"),
                  f"su UnityEventAction NO persiste (persistenceType={pt}); con persistencia la tanda siguiente se reiniciaria sola")

foto = componentes("FotoDeLaEscena")
comprobar(len(foto) == 1, f"hay exactamente una FotoDeLaEscena (hay {len(foto)})")
for fid, body, r in foto:
    ra = [x for x in lista(body, "raices") if x != "0"]
    comprobar(len(ra) >= 1, f"la foto tiene raices asignadas ({len(ra)})")
    for x in ra: print(f"         raiz: {nombre_de(x)}")
    comprobar(campo(body, "incluirComponentes") == "1",
              "incluye componentes (sin esto el lavamanos y el maniqui no vuelven a responder)")

mando = componentes("MandoDelModerador")
comprobar(len(mando) >= 1, f"hay un mando del moderador (hay {len(mando)})", grave=False)

# ---- 5. salidas de los retos
print("\n5. SALIDAS DE LOS RETOS")
llamadores_fade = txt.count("m_MethodName: IniciarFadeOut")
for fid, body, r in componentes("FadeOutVR"):
    ag = campo(body, "avanceEnGrupo")
    if llamadores_fade == 0:
        linea("OK", f"{r}: nadie llama a IniciarFadeOut, el camino antiguo esta muerto")
    else:
        comprobar(ag not in (None, "0"), f"{r}: FadeOutVR delega en el avance en grupo")
for fid, body, r in componentes("GestorCuestionario"):
    comprobar(campo(body, "reunion") not in (None, "0"), f"{r}: el cuestionario usa la reunion")
    comprobar(campo(body, "avanceEnGrupo") not in (None, "0"), f"{r}: el cuestionario usa el avance en grupo")

# ---- 6. restos del camino antiguo
print("\n6. RESTOS DEL CAMINO ANTIGUO (se retiran tras la prueba con dos visores)")
for fid, body, r in componentes("FadeOutVR"):
    n = len(llamadas(body, "OnTeletransporteEjecutado"))
    linea("AVISO" if n else "OK", f"{r}: OnTeletransporteEjecutado conserva {n} entrada(s)")
for fid, body, r in componentes("GestorCuestionario"):
    n = len(llamadas(body, "OnTransitionComplete"))
    linea("AVISO" if n else "OK", f"{r}: OnTransitionComplete conserva {n} entrada(s)")
d3 = componentes("DestinoEscena3Multiusuario")
for fid, body, r in d3:
    a = act.get(go_de.get(fid, ""), "?")
    linea("AVISO" if a == "1" else "OK", f"{r}: DestinoEscena3Multiusuario sigue {'activo' if a=='1' else 'apagado'}")

print("")
print("7. CADENAS QUE TIENEN QUE SEGUIR CONECTADAS")
for metodo, texto in [
    ("Convocar", "la salida del reto 1 convoca a la reunion"),
    ("RegistrarAnuncio", "las acciones de difusion avisan del fin"),
    ("ReiniciarAqui", "la accion de reinicio llega a todos los clientes"),
    ("IniciarTutorialAutomatico", "el splash del reto 3 arranca el video"),
    ("DarPermiso", "el casillero desbloquea el maniqui"),
    ("AbrirPuerta", "el lavamanos abre la puerta"),
]:
    n = txt.count("m_MethodName: " + metodo)
    comprobar(n >= 1, texto + " (" + metodo + ": " + str(n) + " llamador/es)")

print("\n" + "=" * 78)
print(f"RESULTADO: {len(fallos)} fallo(s), {len(avisos)} aviso(s)")
print("=" * 78)
for f in fallos: print("  FALLO:", f)
