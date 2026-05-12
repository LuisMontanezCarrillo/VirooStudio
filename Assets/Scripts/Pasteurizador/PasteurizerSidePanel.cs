using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ViroLab.Pasteurizador
{
    // Panel lateral con arbol de subsistemas + buscador.
    // Cada subsistema tiene un toggle (mostrar/ocultar todas sus partes).
    // El buscador filtra por nombre de parte.
    [DisallowMultipleComponent]
    public class PasteurizerSidePanel : MonoBehaviour
    {
        [Header("Refs scene")]
        public PasteurizerPartsRegistry registry;
        public PasteurizerHoverHandler hover;

        [Header("UI containers")]
        public RectTransform contentRoot;
        public TMP_InputField searchField;

        [Header("Prefabs de fila")]
        public GameObject groupRowPrefab;
        public GameObject partRowPrefab;

        [Header("Buttons")]
        public Button btnShowAll;
        public Button btnHideAll;
        public Button btnFocus;

        private class GroupView
        {
            public string key;
            public GameObject row;
            public Toggle toggle;
            public TMP_Text label;
            public Image colorDot;
            public RectTransform childContainer;
            public List<PartView> parts = new();
            public bool expanded = true;
        }

        private class PartView
        {
            public GameObject go;
            public GameObject row;
            public Toggle toggle;
            public TMP_Text label;
        }

        private readonly List<GroupView> _groups = new();

        private void Awake()
        {
            if (registry == null) registry = GetComponentInParent<PasteurizerPartsRegistry>();
            if (registry == null)
            {
                Debug.LogError("PasteurizerSidePanel: no encuentra registry.");
                enabled = false;
                return;
            }

            BuildTree();
            if (searchField != null) searchField.onValueChanged.AddListener(ApplyFilter);
            if (btnShowAll != null) btnShowAll.onClick.AddListener(() => SetAll(true));
            if (btnHideAll != null) btnHideAll.onClick.AddListener(() => SetAll(false));
            if (btnFocus != null) btnFocus.onClick.AddListener(FocusVisibleOnCamera);
        }

        public void BuildTree()
        {
            foreach (var g in _groups) if (g.row != null) Destroy(g.row);
            _groups.Clear();

            if (registry == null || registry.Database == null) return;

            var keys = new List<string>(registry.BySubsystem.Keys);
            keys.Sort();

            foreach (var key in keys)
            {
                var info = registry.Database.GetByKey(key);
                var list = registry.BySubsystem[key];

                var rowGO = Instantiate(groupRowPrefab, contentRoot);
                var view = new GroupView
                {
                    key = key,
                    row = rowGO,
                    toggle = rowGO.GetComponentInChildren<Toggle>(true),
                    label = FindByNameTMP(rowGO.transform, "Label"),
                    colorDot = FindByName<Image>(rowGO.transform, "ColorDot"),
                    childContainer = FindByName<RectTransform>(rowGO.transform, "Children"),
                };
                if (view.label != null)
                    view.label.text = info != null ? $"{info.title} ({list.Count})" : $"{key} ({list.Count})";
                if (view.colorDot != null && info != null) view.colorDot.color = info.GetColor();

                if (view.toggle != null)
                {
                    view.toggle.isOn = true;
                    var captured = view;
                    view.toggle.onValueChanged.AddListener(v => OnGroupToggle(captured, v));
                }

                if (view.childContainer != null)
                {
                    foreach (var part in list)
                    {
                        var prGO = Instantiate(partRowPrefab, view.childContainer);
                        var pv = new PartView
                        {
                            go = part,
                            row = prGO,
                            toggle = prGO.GetComponentInChildren<Toggle>(true),
                            label = FindByNameTMP(prGO.transform, "Label"),
                        };
                        if (pv.label != null) pv.label.text = part.name;
                        if (pv.toggle != null)
                        {
                            pv.toggle.isOn = true;
                            var captured = pv;
                            pv.toggle.onValueChanged.AddListener(v => captured.go.SetActive(v));
                        }
                        var btn = prGO.GetComponentInChildren<Button>(true);
                        if (btn != null)
                        {
                            var go = part;
                            btn.onClick.AddListener(() => FocusOn(go));
                        }
                        view.parts.Add(pv);
                    }
                }
                _groups.Add(view);
            }
        }

        private void OnGroupToggle(GroupView g, bool on)
        {
            foreach (var p in g.parts)
            {
                if (p.toggle != null) p.toggle.isOn = on;
                p.go.SetActive(on);
            }
        }

        private void SetAll(bool on)
        {
            foreach (var g in _groups)
            {
                if (g.toggle != null) g.toggle.isOn = on;
                foreach (var p in g.parts)
                {
                    if (p.toggle != null) p.toggle.isOn = on;
                    p.go.SetActive(on);
                }
            }
        }

        private void ApplyFilter(string query)
        {
            query = query == null ? "" : query.Trim().ToLowerInvariant();
            foreach (var g in _groups)
            {
                bool anyVisible = false;
                foreach (var p in g.parts)
                {
                    bool show = query.Length == 0 || p.go.name.ToLowerInvariant().Contains(query);
                    if (p.row != null) p.row.SetActive(show);
                    if (show) anyVisible = true;
                }
                if (g.row != null) g.row.SetActive(anyVisible);
            }
        }

        private void FocusOn(GameObject go)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var c = b.center;
            var d = Mathf.Max(b.size.x, b.size.y, b.size.z) * 2.5f;
            cam.transform.position = c + cam.transform.forward * -d;
            cam.transform.LookAt(c);
        }

        private void FocusVisibleOnCamera()
        {
            var cam = Camera.main;
            if (cam == null || registry == null) return;
            var b = new Bounds();
            bool init = false;
            foreach (var kvp in registry.ByName)
            {
                if (!kvp.Value.activeInHierarchy) continue;
                var r = kvp.Value.GetComponent<Renderer>();
                if (r == null) continue;
                if (!init) { b = r.bounds; init = true; }
                else b.Encapsulate(r.bounds);
            }
            if (!init) return;
            var d = Mathf.Max(b.size.x, b.size.y, b.size.z) * 1.6f;
            cam.transform.position = b.center + cam.transform.forward * -d;
            cam.transform.LookAt(b.center);
        }

        private static T FindByName<T>(Transform root, string name) where T : Component
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.GetComponent<T>();
            return null;
        }

        private static TMP_Text FindByNameTMP(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                {
                    var tmp = t.GetComponent<TMP_Text>();
                    if (tmp != null) return tmp;
                }
            return null;
        }
    }
}
