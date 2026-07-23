#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ViroLab.Pasteurizador.Simulator;

namespace ViroLab.Pasteurizador.EditorTools
{
    /// Construye el diagrama "Plant" (SVG portado a UGUI) dentro de un
    /// RectTransform dado. Coordenadas internas 0..1280 x 0..720 (igual
    /// al viewBox del SVG original) — el helper Px() los escala al área.
    ///
    /// Devuelve un objeto con todas las referencias para asignar al
    /// PasteurizerSimDashboard.
    public static class PasteurizerSimPlantBuilder
    {
        public class PlantRefs
        {
            public Image tankInFill, tankOutFill, pulmonFill;
            public TMP_Text tankInVolTxt, tankOutVolTxt, retDisplay;
            public RectTransform pumpMilkBlade, pumpHotBlade;
            public Image flameIcon, refriLed, trapDrip;
            public RectTransform psiNeedle;
            public TMP_Text psiText;
            public Image retFlow;
            public Image pipeInlet;
            public Image[] pipeRaw, pipeToHeat, pipeToRet, pipeFromRet, pipeToCooling, pipeOut, pipeReturn;
            public Image[] pipeHotSupply, pipeHotReturn, pipeHotBack;
            public Image[] pipeCold, pipeColdReturn, pipeMakeup, pipePulmonOut;
            public RectTransform valveProdHandle, valveRetHandle, valveDesHandle, valveFillHandle;
            public Image valveProdBody, valveRetBody, valveDesBody, valveFillBody;
        }

        // viewBox SVG original
        private const float SVG_W = 1280f;
        private const float SVG_H = 720f;
        private const float PIPE_THICKNESS = 5f;  // grosor de tubería en px de canvas

        // Paleta
        private static readonly Color CPipeOff   = new Color(0.30f, 0.34f, 0.38f, 1f);
        // Etiquetas en NEGRO: el fondo del diagrama es claro y antes (azul-gris) casi no se leían.
        private static readonly Color CLabel     = new Color(0.06f, 0.08f, 0.10f, 1f);
        private static readonly Color CLabelDim  = new Color(0.22f, 0.25f, 0.29f, 1f);
        private static readonly Color CTankShell = new Color(0.78f, 0.83f, 0.88f, 1f);
        private static readonly Color CTankBack  = new Color(0.05f, 0.07f, 0.10f, 1f);
        private static readonly Color CMilkFill  = new Color(1.00f, 0.96f, 0.85f, 1f);
        private static readonly Color CMetal     = new Color(0.55f, 0.60f, 0.67f, 1f);
        private static readonly Color CMetalDk   = new Color(0.18f, 0.22f, 0.27f, 1f);
        private static readonly Color CPlateBg   = new Color(0.55f, 0.62f, 0.72f, 1f);
        private static readonly Color CPlateHeat = new Color(1.00f, 0.55f, 0.30f, 0.18f);
        private static readonly Color CPlateRegen = new Color(0.77f, 0.40f, 1.00f, 0.15f);
        private static readonly Color CPlateCool = new Color(0.40f, 0.65f, 1.00f, 0.15f);

        private static RectTransform _root;
        private static float _scaleX, _scaleY;

        public static PlantRefs Build(Transform parent, Vector2 areaSize)
        {
            // Contenedor del plant
            var rootGO = new GameObject("PlantSVG", typeof(RectTransform));
            rootGO.transform.SetParent(parent, false);
            _root = (RectTransform)rootGO.transform;
            _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero; _root.offsetMax = Vector2.zero;

            _scaleX = areaSize.x / SVG_W;
            _scaleY = areaSize.y / SVG_H;

            // Fondo del plant
            var bg = AddImage(_root, "Bg", new Color(0.93f, 0.95f, 0.98f, 1f));
            Stretch(bg.rectTransform);

            var refs = new PlantRefs();

            // ---- TUBERÍAS (dibujadas primero para que queden detrás) ----
            refs.pipeMakeup     = Pipes("pipeMakeup",   _root, new Vector2[]{ V(1260,700), V(1220,700), V(1220,560) });
            refs.pipePulmonOut  = Pipes("pipePulmonOut",_root, new Vector2[]{ V(1180,480), V(1180,360), V(1150,360) });

            refs.pipeInlet      = PipeSingle("pipeInlet", _root, new Vector2[]{ V(40,60), V(160,60), V(160,290) });
            refs.pipeRaw        = Pipes("pipeRaw",       _root, new Vector2[]{ V(210,470), V(260,470), V(260,540), V(320,540), V(320,470), V(495,470) });
            refs.pipeToHeat     = Pipes("pipeToHeat",    _root, new Vector2[]{ V(780,470), V(820,470), V(820,320), V(495,320), V(495,240), V(780,240) });
            refs.pipeToRet      = Pipes("pipeToRet",     _root, new Vector2[]{ V(780,240), V(880,240), V(880,200) });
            refs.pipeFromRet    = Pipes("pipeFromRet",   _root, new Vector2[]{ V(960,200), V(960,280), V(780,280), V(780,380), V(495,380) });
            refs.pipeToCooling  = Pipes("pipeToCooling", _root, new Vector2[]{ V(495,530), V(460,530), V(460,600), V(820,600), V(820,560), V(780,560) });
            refs.pipeOut        = Pipes("pipeOut",       _root, new Vector2[]{ V(495,560), V(380,560), V(380,130), V(560,130) });
            refs.pipeReturn     = Pipes("pipeReturn",    _root, new Vector2[]{ V(460,600), V(260,600), V(260,270), V(160,270) });
            refs.pipeHotSupply  = Pipes("pipeHotSupply", _root, new Vector2[]{ V(1110,470), V(1110,580), V(1080,580) });
            refs.pipeHotReturn  = Pipes("pipeHotReturn", _root, new Vector2[]{ V(1020,580), V(960,580), V(960,420), V(780,420) });
            refs.pipeHotBack    = Pipes("pipeHotBack",   _root, new Vector2[]{ V(780,200), V(820,200), V(820,160), V(1110,160), V(1110,280) });
            refs.pipeCold       = Pipes("pipeCold",      _root, new Vector2[]{ V(295,615), V(295,560), V(495,560) });
            refs.pipeColdReturn = Pipes("pipeColdReturn",_root, new Vector2[]{ V(495,600), V(260,600), V(260,645) });

            // ---- HOLDING TUBE simplificado (rect alargada con etiqueta) ----
            BuildHoldingTube(_root, refs);

            // ---- TANQUE ENTRADA ----
            BuildTankIn(_root, refs);

            // ---- TANQUE FINAL ----
            BuildTankOut(_root, refs);

            // ---- INTERCAMBIADOR DE PLACAS ----
            BuildPlateHX(_root);

            // ---- CALDERA ----
            BuildBoiler(_root, refs);

            // ---- BOMBA MILK ----
            refs.pumpMilkBlade = BuildPump(_root, "PumpMilk", new Vector2(290, 540), "BOMBA DE PRODUCTO");

            // ---- BOMBA HOT WATER ----
            refs.pumpHotBlade = BuildPump(_root, "PumpHot",  new Vector2(1050, 580), "BOMBA DE AGUA");

            // ---- REFRIGERADOR ----
            BuildRefri(_root, refs);

            // ---- TRAMPA VAPOR ----
            BuildTrap(_root, refs);

            // ---- TANQUE PULMÓN ----
            BuildPulmon(_root, refs);

            // ---- MANÓMETRO ----
            BuildPSIGauge(_root, refs);

            // ---- VÁLVULAS (encima de las tuberías) ----
            BuildValve(_root, "valveProd",  V(380,380), "V. PRODUCTO",   out refs.valveProdBody, out refs.valveProdHandle);
            BuildValve(_root, "valveDes",   V(460,600), "V. DESVIACION", out refs.valveDesBody,  out refs.valveDesHandle);
            BuildValve(_root, "valveRet",   V(260,380), "V. RETORNO",    out refs.valveRetBody,  out refs.valveRetHandle);
            BuildValve(_root, "valveFill",  V(160,230), "V. LLENADO",    out refs.valveFillBody, out refs.valveFillHandle);

            return refs;
        }

        // ====================================================================
        //  Conversión coords SVG → canvas (Y invertida)
        // ====================================================================
        private static Vector2 V(float svgX, float svgY)
        {
            return new Vector2(svgX, SVG_H - svgY);
        }
        private static float Px(float svgUnit) => svgUnit * _scaleX;
        private static float Py(float svgUnit) => svgUnit * _scaleY;

        // ====================================================================
        //  Tubería como una secuencia de segments rectangulares
        // ====================================================================
        private static Image[] Pipes(string name, Transform parent, Vector2[] pts)
        {
            var holder = new GameObject(name, typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            var hrt = (RectTransform)holder.transform;
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;

            var list = new List<Image>();
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var seg = PipeSegment(holder.transform, $"seg{i}", pts[i], pts[i + 1]);
                list.Add(seg);
            }
            return list.ToArray();
        }

        private static Image PipeSingle(string name, Transform parent, Vector2[] pts)
        {
            // Para tuberías chicas con 1 sola imagen (anidamos los segments)
            // Si hay >1 segments igual los devolvemos pero el dashboard solo anima la 1ra.
            var arr = Pipes(name, parent, pts);
            return arr.Length > 0 ? arr[0] : null;
        }

        private static Image PipeSegment(Transform parent, string name, Vector2 a, Vector2 b)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 ap = new Vector2(Px(a.x), Py(a.y));
            Vector2 bp = new Vector2(Px(b.x), Py(b.y));
            Vector2 mid = (ap + bp) * 0.5f;
            float length = Vector2.Distance(ap, bp);
            float angle = Mathf.Atan2(bp.y - ap.y, bp.x - ap.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(length + PIPE_THICKNESS, PIPE_THICKNESS);
            rt.localEulerAngles = new Vector3(0, 0, angle);

            var img = go.GetComponent<Image>();
            img.color = CPipeOff;
            img.raycastTarget = false;
            return img;
        }

        // ====================================================================
        //  TANQUES y demás equipos
        // ====================================================================
        private static void BuildTankIn(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "TankIn", V(90, 290), Px(140), Py(180));
            // shell
            AddRect(g, "Shell", Vector2.zero, new Vector2(Px(140), Py(180)), CTankShell);
            // ventana vidrio
            AddRect(g, "Window", new Vector2(Px(15), -Py(15)), new Vector2(Px(22), Py(150)), CTankBack);
            // fill (vertical filled)
            var fill = AddImage(g, "Fill", CMilkFill);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.zero;
            frt.pivot = new Vector2(0, 1);
            frt.anchoredPosition = new Vector2(Px(16), -Py(15));
            frt.sizeDelta = new Vector2(Px(20), Py(150));
            refs.tankInFill = fill;

            // label
            AddLabelAt(parent, "ENTRADA DEL PRODUCTO", V(160, 280), 16, CLabel);
            AddLabelAt(parent, "Cap. 100 L", V(90, 540), 15, CLabelDim);

            // display volumen
            var disp = AddDisplayPanel(parent, V(90, 548), Px(110), Py(22));
            refs.tankInVolTxt = AddDisplayText(disp, "0.0 L");
        }

        private static void BuildTankOut(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "TankOut", V(560, 50), Px(120), Py(110));
            // shell
            AddRect(g, "Shell", Vector2.zero, new Vector2(Px(120), Py(110)), CTankShell);
            // tapa elipse aproximada (rect achatado arriba)
            AddRect(g, "TopCap", new Vector2(0, Py(0)), new Vector2(Px(120), Py(20)), CTankShell);
            // ventana vidrio
            AddRect(g, "Window", new Vector2(Px(8), -Py(10)), new Vector2(Px(20), Py(90)), CTankBack);
            // fill
            var fill = AddImage(g, "Fill", CMilkFill);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0f;
            var frt = fill.rectTransform;
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.zero;
            frt.pivot = new Vector2(0, 1);
            frt.anchoredPosition = new Vector2(Px(9), -Py(10));
            frt.sizeDelta = new Vector2(Px(18), Py(90));
            refs.tankOutFill = fill;

            AddLabelAt(parent, "PRODUCTO FINAL", V(540, 40), 16, CLabel);
            AddLabelAt(parent, "Cap. 150 L", V(700, 80), 15, CLabelDim);
            var disp = AddDisplayPanel(parent, V(690, 86), Px(80), Py(20));
            refs.tankOutVolTxt = AddDisplayText(disp, "0.0 L");
        }

        private static void BuildPlateHX(Transform parent)
        {
            var g = NewAt(parent, "PlateHX", V(490, 200), Px(290), Py(380));
            AddRect(g, "Body", Vector2.zero, new Vector2(Px(290), Py(380)), CPlateBg);
            // zonas color
            AddRect(g, "Heat",  new Vector2(0, 0),         new Vector2(Px(290), Py(100)), CPlateHeat);
            AddRect(g, "Regen", new Vector2(0, -Py(100)),  new Vector2(Px(290), Py(160)), CPlateRegen);
            AddRect(g, "Cool",  new Vector2(0, -Py(260)),  new Vector2(Px(290), Py(120)), CPlateCool);
            // líneas verticales (placas)
            for (int i = 0; i < 19; i++)
            {
                AddRect(g, $"plate{i}",
                    new Vector2(Px(20 + i * 15), -Py(5)),
                    new Vector2(1.5f, Py(370)),
                    new Color(0.30f, 0.34f, 0.40f, 0.7f));
            }
            AddLabelAt(parent, "INTERCAMBIADOR DE PLACAS", V(635, 190), 16, CLabel, TextAlignmentOptions.Center);
            AddLabelAt(parent, "CALENTAMIENTO", V(800, 250), 14, CLabelDim);
            AddLabelAt(parent, "REGENERACION",  V(800, 380), 14, CLabelDim);
            AddLabelAt(parent, "ENFRIAMIENTO",  V(800, 520), 14, CLabelDim);
        }

        private static void BuildHoldingTube(Transform parent, PlantRefs refs)
        {
            // Versión simplificada: 4 rectángulos horizontales apilados con corner joints
            var g = NewAt(parent, "HoldingTube", V(806, 80), Px(200), Py(160));
            // base
            AddRect(g, "Bg", Vector2.zero, new Vector2(Px(200), Py(160)), new Color(0.78f, 0.83f, 0.88f, 1f));
            // serpentina representada como 4 stripes horizontales
            var serpBg = AddRect(g, "Serp", new Vector2(Px(10), -Py(15)), new Vector2(Px(180), Py(130)),
                new Color(0.50f, 0.55f, 0.60f, 1f));
            // overlay flow (se muestra cuando hay flujo caliente)
            var flowOverlay = AddImage(g, "FlowOverlay", new Color(1f, 0.48f, 0.30f, 1f));
            flowOverlay.rectTransform.anchorMin = Vector2.zero;
            flowOverlay.rectTransform.anchorMax = Vector2.zero;
            flowOverlay.rectTransform.pivot = new Vector2(0, 1);
            flowOverlay.rectTransform.anchoredPosition = new Vector2(Px(12), -Py(18));
            flowOverlay.rectTransform.sizeDelta = new Vector2(Px(176), Py(124));
            var c = flowOverlay.color; c.a = 0f; flowOverlay.color = c;
            refs.retFlow = flowOverlay;

            AddLabelAt(parent, "RETENCION", V(820, 80), 16, CLabel);

            // Display tiempo retención
            var disp = AddDisplayPanel(parent, V(900, 180), Px(70), Py(30));
            refs.retDisplay = AddDisplayText(disp, "0");
        }

        private static void BuildBoiler(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "Boiler", V(1040, 280), Px(140), Py(200));
            AddRect(g, "Body", Vector2.zero, new Vector2(Px(140), Py(200)), CMetal);
            // chimenea
            AddRect(g, "Stack", new Vector2(Px(55), Py(60)), new Vector2(Px(30), Py(65)), CMetalDk);
            // tubos calientes (rojos) + fríos (azules)
            for (int i = 0; i < 3; i++)
                AddRect(g, $"hotPipe{i}", new Vector2(Px(12), -Py(18 + i * 18)),
                    new Vector2(Px(116), Py(14)), new Color(0.89f, 0.23f, 0.23f, 1f));
            for (int i = 0; i < 3; i++)
                AddRect(g, $"coldPipe{i}", new Vector2(Px(12), -Py(72 + i * 18)),
                    new Vector2(Px(116), Py(14)), new Color(0.12f, 0.36f, 0.82f, 1f));

            // Flama (elipse aproximada con varios rects)
            var flame = AddImage(g, "Flame", new Color(1f, 0.62f, 0.20f, 0.9f));
            flame.rectTransform.anchorMin = Vector2.zero; flame.rectTransform.anchorMax = Vector2.zero;
            flame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            flame.rectTransform.anchoredPosition = new Vector2(Px(70), -Py(170));
            flame.rectTransform.sizeDelta = new Vector2(Px(90), Py(40));
            refs.flameIcon = flame;

            AddLabelAt(parent, "CALDERA", V(1085, 270), 16, CLabel);
        }

        private static RectTransform BuildPump(Transform parent, string name, Vector2 svgCenter, string label)
        {
            // svgCenter es centro del pump
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Px(svgCenter.x), Py(SVG_H - svgCenter.y));
            rt.sizeDelta = new Vector2(Px(60), Py(44));

            // Caja
            AddRect(rt, "Box", Vector2.zero, new Vector2(Px(60), Py(44)), CMetal);
            // Círculo central (aproximamos con rect cuadrado)
            AddRect(rt, "Hub", Vector2.zero, new Vector2(Px(40), Py(40)), CMetalDk);

            // Aspas (dos líneas perpendiculares)
            var blades = new GameObject("Blades", typeof(RectTransform));
            blades.transform.SetParent(rt, false);
            var brt = (RectTransform)blades.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(Px(26), Py(26));
            AddRect(brt, "BladeH", Vector2.zero, new Vector2(Px(26), 3f), Color.white);
            AddRect(brt, "BladeV", Vector2.zero, new Vector2(3f, Py(26)), Color.white);

            AddLabelAt(parent, label,
                new Vector2(svgCenter.x - 45f, SVG_H - svgCenter.y - 60f), 9, CLabelDim);

            return brt;
        }

        private static void BuildRefri(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "Refri", V(240, 615), Px(110), Py(60));
            AddRect(g, "Body", Vector2.zero, new Vector2(Px(110), Py(60)), CTankShell);
            AddRect(g, "Inner", new Vector2(Px(8), -Py(8)), new Vector2(Px(94), Py(44)), CMetal);
            // LED
            var led = AddImage(g, "Led", new Color(0.22f, 0.26f, 0.32f));
            led.rectTransform.anchorMin = Vector2.zero; led.rectTransform.anchorMax = Vector2.zero;
            led.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            led.rectTransform.anchoredPosition = new Vector2(Px(95), -Py(10));
            led.rectTransform.sizeDelta = new Vector2(8, 8);
            refs.refriLed = led;
            AddLabelAt(parent, "REFRIGERADOR", V(245, 697), 14, CLabelDim);
        }

        private static void BuildTrap(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "Trap", V(862, 526), Px(36), Py(28));
            AddRect(g, "Body", Vector2.zero, new Vector2(Px(36), Py(28)), CTankShell);
            AddRect(g, "Core", new Vector2(Px(10), -Py(6)), new Vector2(Px(16), Py(16)), CMetalDk);
            // gotita (texto)
            var dripGO = new GameObject("Drip", typeof(RectTransform), typeof(Image));
            dripGO.transform.SetParent(g, false);
            var drt = (RectTransform)dripGO.transform;
            drt.anchorMin = drt.anchorMax = Vector2.zero;
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(Px(18), -Py(30));
            drt.sizeDelta = new Vector2(6, 10);
            var drip = dripGO.GetComponent<Image>();
            drip.color = new Color(0.12f, 0.36f, 0.82f, 0f);
            refs.trapDrip = drip;
            AddLabelAt(parent, "TRAMPA VAPOR", V(850, 504), 14, CLabelDim);
        }

        private static void BuildPulmon(Transform parent, PlantRefs refs)
        {
            var g = NewAt(parent, "Pulmon", V(1190, 430), Px(60), Py(140));
            AddRect(g, "Shell", Vector2.zero, new Vector2(Px(60), Py(140)), CTankShell);
            AddRect(g, "Window", new Vector2(Px(8), -Py(12)), new Vector2(Px(14), Py(120)), CTankBack);
            // fill vertical
            var fill = AddImage(g, "Fill", new Color(0.62f, 0.83f, 1f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = (int)Image.OriginVertical.Bottom;
            fill.fillAmount = 0.5f;
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.zero;
            fill.rectTransform.pivot = new Vector2(0, 1);
            fill.rectTransform.anchoredPosition = new Vector2(Px(9), -Py(12));
            fill.rectTransform.sizeDelta = new Vector2(Px(12), Py(120));
            refs.pulmonFill = fill;
            // marca 70%
            AddRect(g, "OptLine", new Vector2(Px(6), -Py(48)), new Vector2(Px(56), 1.5f),
                new Color(0.89f, 0.63f, 0.23f, 1f));
            AddLabelAt(parent, "TANQUE PULMON", V(1190, 425), 14, CLabel);
            AddLabelAt(parent, "Nivel Optimo",   V(1196, 475), 13, new Color(0.66f, 0.43f, 0.06f, 1f));
        }

        private static void BuildPSIGauge(Transform parent, PlantRefs refs)
        {
            float r = 22f;
            var g = NewAt(parent, "PSIGauge", V(1110 - r, 220 - r), Px(r * 2), Py(r * 2));
            // círculo (rect cuadrado claro)
            AddRect(g, "Face", Vector2.zero, new Vector2(Px(r * 2), Py(r * 2)), new Color(0.98f, 0.98f, 1f));
            // hub central
            var hub = AddImage(g, "Hub", CMetalDk);
            hub.rectTransform.anchorMin = hub.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            hub.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hub.rectTransform.anchoredPosition = Vector2.zero;
            hub.rectTransform.sizeDelta = new Vector2(6, 6);
            // aguja
            var needleGO = new GameObject("Needle", typeof(RectTransform));
            needleGO.transform.SetParent(g, false);
            var nrt = (RectTransform)needleGO.transform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0.5f, 0.5f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = Vector2.zero;
            nrt.sizeDelta = new Vector2(2, Py(16));
            var needleImg = needleGO.AddComponent<Image>();
            needleImg.color = new Color(0.89f, 0.23f, 0.23f);
            refs.psiNeedle = nrt;
            // PSI text encima
            var txtGO = new GameObject("PSITxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(g, false);
            var trt = (RectTransform)txtGO.transform;
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 0f);
            trt.anchoredPosition = new Vector2(0, 2);
            trt.sizeDelta = new Vector2(40, 14);
            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "0"; tmp.fontSize = 10; tmp.color = CLabelDim; tmp.alignment = TextAlignmentOptions.Center;
            refs.psiText = tmp;
            AddLabelAt(parent, "PSI", V(1110 - 8, 263), 14, CLabelDim);
        }

        private static void BuildValve(Transform parent, string name, Vector2 svgCenter, string label,
                                       out Image body, out RectTransform handle)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Px(svgCenter.x), Py(svgCenter.y));
            rt.sizeDelta = new Vector2(Px(28), Py(28));

            // Cuerpo (círculo aproximado con rect cuadrado)
            var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGO.transform.SetParent(rt, false);
            var brt = (RectTransform)bodyGO.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(Px(28), Py(28));
            body = bodyGO.GetComponent<Image>();
            body.color = new Color(0.78f, 0.83f, 0.88f);

            // Handle "+" centro (2 rects perpendiculares) — rota 45° al abrir
            var handleGO = new GameObject("Handle", typeof(RectTransform));
            handleGO.transform.SetParent(rt, false);
            var hrt = (RectTransform)handleGO.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(Px(18), Py(18));
            AddRect(hrt, "H", Vector2.zero, new Vector2(Px(18), 3f), CMetalDk);
            AddRect(hrt, "V", Vector2.zero, new Vector2(3f, Py(18)), CMetalDk);
            handle = hrt;

            AddLabelAt(parent, label,
                new Vector2(svgCenter.x + 14, svgCenter.y + 12), 8, CLabelDim);
        }

        // ====================================================================
        //  Helpers
        // ====================================================================
        private static RectTransform NewAt(Transform parent, string name, Vector2 svgTopLeft, float w, float h)
        {
            // svgTopLeft viene ya en coordenadas V() (Y invertida del top)
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(Px(svgTopLeft.x), Py(svgTopLeft.y));
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        private static Image AddRect(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Image AddImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static TMP_Text AddLabelAt(Transform parent, string text, Vector2 svgPos,
            int size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            // Contenedor con FONDO CLARO que se ajusta al texto: así la etiqueta se lee
            // aunque quede encima de una tubería.
            var go = new GameObject("Lbl_" + text, typeof(RectTransform), typeof(Image),
                                    typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(Px(svgPos.x), Py(SVG_H - svgPos.y));

            var chip = go.GetComponent<Image>();
            chip.color = new Color(1f, 1f, 1f, 0.85f);
            chip.raycastTarget = false;

            var hlg = go.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 2, 2);
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            var fit = go.GetComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            var tgo = new GameObject("Txt", typeof(RectTransform), typeof(TextMeshProUGUI));
            tgo.transform.SetParent(go.transform, false);
            var tmp = tgo.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = false;
            return tmp;
        }

        private static RectTransform AddDisplayPanel(Transform parent, Vector2 svgTopLeft, float w, float h)
        {
            var go = new GameObject("DispPanel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(Px(svgTopLeft.x), Py(SVG_H - svgTopLeft.y));
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.05f, 0.07f, 0.10f, 1f);
            img.raycastTarget = false;
            return rt;
        }

        private static TMP_Text AddDisplayText(Transform parent, string text)
        {
            var go = new GameObject("DispTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 11;
            tmp.color = new Color(0f, 0.88f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
#endif
