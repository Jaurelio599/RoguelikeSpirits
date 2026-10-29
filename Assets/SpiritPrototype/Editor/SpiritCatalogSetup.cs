using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritCatalogSetup
    {
        public const string CatalogPath = "Assets/SpiritPrototype/Resources/SpiritPrototype/ContentCatalog.asset";
        public const string ContentRoot = "Assets/SpiritPrototype/Content";
        [MenuItem("Tools/Espiritus/8 Preparar catalogos editables")]
        public static void Prepare()
        {
            Directory.CreateDirectory(ContentRoot + "/Spirits"); Directory.CreateDirectory(ContentRoot + "/Relics");
            Directory.CreateDirectory("Assets/SpiritPrototype/Resources/SpiritPrototype"); AssetDatabase.Refresh();
            var catalog = AssetDatabase.LoadAssetAtPath<SpiritContentCatalog>(CatalogPath);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<SpiritContentCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
            var species = new List<SpiritDefinition>(catalog.spirits ?? new SpiritDefinition[0]);
            var relics = new List<RelicDefinition>(catalog.relics ?? new RelicDefinition[0]);
            var actors = UnityEngine.Object.FindObjectsByType<SpiritEncounter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (string id in PrototypeContentDefaults.CoreSpecies)
            {
                if (species.Exists(s => s != null && s.id == id)) continue;
                string path = ContentRoot + "/Spirits/" + id.Replace(' ', '_') + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<SpiritDefinition>(path);
                if (definition == null)
                {
                    definition = PrototypeContentDefaults.Spirit(id);
                    string spriteName = id == "Guardian del Umbral" ? "Raicero" : id;
                    definition.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Characters/" + spriteName + ".png");
                    foreach (var actor in actors)
                        if (actor.species == id)
                        {
                            definition.enemyMaxHealth = actor.maxHealth; definition.enemyAttack = actor.attackDamage;
                            definition.enemyStability = actor.maxStability; definition.capturable = actor.capturable;
                            if (actor.portrait != null) definition.sprite = actor.portrait;
                            break;
                        }
                    AssetDatabase.CreateAsset(definition, path);
                }
                species.Add(definition);
            }
            string[] icons = {"CarbonTallado", "LenteVoraz", "PrismaVecino"};
            for (int id = 0; id < 7; id++)
            {
                if (relics.Exists(r => r != null && r.id == id)) continue;
                string path = ContentRoot + "/Relics/Relic_" + id + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
                if (definition == null)
                {
                    definition = PrototypeContentDefaults.Relic(id);
                    if (id < icons.Length) definition.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SpiritPrototype/Generated/Items/" + icons[id] + ".png");
                    AssetDatabase.CreateAsset(definition, path);
                }
                relics.Add(definition);
            }
            catalog.spirits = species.ToArray(); catalog.relics = relics.ToArray();
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); PrototypeContent.Reload();
            string error = catalog.ValidateCatalog();
            Selection.activeObject = catalog;
            EditorUtility.DisplayDialog(error.Length == 0 ? "Catalogos preparados" : "Revisa el catalogo", error.Length == 0
                ? "Las fichas estan en Assets/SpiritPrototype/Content. Los menus Catalogos permiten crear y registrar contenido nuevo. Repetir este paso conserva las fichas existentes."
                : error, "Aceptar");
        }
        [MenuItem("Tools/Espiritus/8 Preparar catalogos editables", true)]
        private static bool CanPrepare() => !EditorApplication.isPlayingOrWillChangePlaymode;
        [MenuItem("Tools/Espiritus/Catalogos/Seleccionar catalogo")]
        private static void SelectCatalog() { Selection.activeObject = AssetDatabase.LoadAssetAtPath<SpiritContentCatalog>(CatalogPath); }

        internal static string Slug(string name)
        {
            var builder = new StringBuilder();
            foreach (char c in name.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
                else if (builder.Length > 0 && builder[builder.Length - 1] != '_') builder.Append('_');
            }
            string value = builder.ToString().Trim('_'); return value.Length == 0 ? "espiritu" : value;
        }
    }

    public sealed class SpiritContentCreateWindow : EditorWindow
    {
        private bool relic;
        private string displayName = "Nuevo espiritu", message = "";
        [MenuItem("Tools/Espiritus/Catalogos/Crear espiritu")]
        private static void OpenSpirit() { var w = GetWindow<SpiritContentCreateWindow>(true, "Crear espiritu"); w.relic = false; w.displayName = "Nuevo espiritu"; w.minSize = new Vector2(430, 220); }
        [MenuItem("Tools/Espiritus/Catalogos/Crear reliquia")]
        private static void OpenRelic() { var w = GetWindow<SpiritContentCreateWindow>(true, "Crear reliquia"); w.relic = true; w.displayName = "Nueva reliquia"; w.minSize = new Vector2(430, 220); }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("La ficha se registrara automaticamente en el catalogo.");
            displayName = EditorGUILayout.TextField("Nombre visible", displayName);
            EditorGUILayout.HelpBox(relic ? "Se asigna un ID numerico nuevo. Despues puedes elegir efecto, condicion e icono en el Inspector."
                : "Se asigna un ID de texto unico. Despues puedes editar sprite, rareza y atributos en el Inspector.", MessageType.Info);
            bool old = GUI.enabled; GUI.enabled = old && !EditorApplication.isPlayingOrWillChangePlaymode && !string.IsNullOrWhiteSpace(displayName);
            if (GUILayout.Button("Crear ficha y registrar")) Create();
            GUI.enabled = old;
            EditorGUILayout.LabelField(message);
        }
        private void Create()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SpiritContentCatalog>(SpiritCatalogSetup.CatalogPath);
            if (catalog == null) { message = "Ejecuta primero Tools > Espiritus > 8 Preparar catalogos editables."; return; }
            if (catalog.ValidateCatalog().Length > 0) { message = catalog.ValidateCatalog(); return; }
            UnityEngine.Object created;
            if (relic)
            {
                int next = 7;
                foreach (var existing in catalog.relics) next = Math.Max(next, existing.id + 1);
                var definition = ScriptableObject.CreateInstance<RelicDefinition>(); definition.id = next; definition.displayName = displayName.Trim();
                var list = new List<RelicDefinition>(catalog.relics) {definition}; catalog.relics = list.ToArray(); created = definition;
                AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(SpiritCatalogSetup.ContentRoot + "/Relics/Relic_" + next + ".asset"));
            }
            else
            {
                string basic = SpiritCatalogSetup.Slug(displayName), id = basic; int suffix = 2;
                var used = new HashSet<string>(); foreach (var existing in catalog.spirits) used.Add(existing.id);
                while (used.Contains(id)) id = basic + "_" + suffix++;
                var definition = ScriptableObject.CreateInstance<SpiritDefinition>(); definition.id = id; definition.displayName = displayName.Trim();
                foreach (var existing in catalog.spirits) if (existing.id == "Velo") definition.sprite = existing.sprite;
                var list = new List<SpiritDefinition>(catalog.spirits) {definition}; catalog.spirits = list.ToArray(); created = definition;
                AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(SpiritCatalogSetup.ContentRoot + "/Spirits/" + id + ".asset"));
            }
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets(); PrototypeContent.Reload(); Selection.activeObject = created;
            message = "Ficha creada y registrada. Ajusta sus valores en el Inspector y guarda el proyecto.";
        }
    }
}
