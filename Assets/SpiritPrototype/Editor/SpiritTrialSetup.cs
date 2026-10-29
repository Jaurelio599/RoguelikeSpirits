using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class SpiritTrialSetup
    {
        private const string Root = "Assets/SpiritPrototype";
        [MenuItem("Tools/Espiritus/10 Animar espiritus y completar pruebas")]
        private static void Setup()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SpiritContentCatalog>(SpiritCatalogSetup.CatalogPath);
            if (catalog == null) { EditorUtility.DisplayDialog("Falta el catalogo", "Ejecuta primero el paso 8 de catalogos editables.", "Aceptar"); return; }
            try
            {
                string error = catalog.ValidateCatalog(); if (error.Length > 0) throw new InvalidOperationException(error);
                var idle = BakeAtlas(Root + "/Source/SpiritIdleSheet_v01.png", 1024, 1536,
                    new[] {0, 270, 520, 770, 1024}, new[] {0, 277, 538, 770, 990, 1256, 1536}, Root + "/Generated/SpiritIdle", true);
                var auxiliary = BakeAtlas(Root + "/Source/AuxiliaryRelics_v01.png", 1358, 1159,
                    new[] {0, 679, 1358}, new[] {0, 579, 1159}, Root + "/Generated/AuxiliaryRelics", false);
                string[] ids = {"Ascua", "Velo", "Raicero", "Campanilla", "Luto", "Farol"};
                foreach (var spirit in catalog.spirits)
                {
                    int row = Array.IndexOf(ids, spirit.id == "Guardian del Umbral" ? "Raicero" : spirit.id);
                    if (row < 0) continue;
                    if (spirit.idleFrames == null || spirit.idleFrames.Length == 0)
                    {
                        if (spirit.idleFramesPerSecond <= 0 || float.IsNaN(spirit.idleFramesPerSecond) || float.IsInfinity(spirit.idleFramesPerSecond)) spirit.idleFramesPerSecond = 5;
                        spirit.idleFrames = new[] {idle[row * 4], idle[row * 4 + 1], idle[row * 4 + 2], idle[row * 4 + 3]};
                        EditorUtility.SetDirty(spirit);
                    }
                }
                var relics = new List<RelicDefinition>(catalog.relics); int next = 7;
                foreach (var relic in relics) next = Math.Max(next, relic.id + 1);
                Directory.CreateDirectory(Root + "/Content/Relics"); AssetDatabase.Refresh();
                int added = 0;
                for (int i = 0; i < PrototypeContentDefaults.TrialKeys.Length; i++)
                {
                    string key = "prototype." + PrototypeContentDefaults.TrialKeys[i];
                    var definition = relics.Find(r => r.prototypeKey == key);
                    if (definition == null)
                    {
                        definition = PrototypeContentDefaults.TrialRelic(i, next++); relics.Add(definition); added++;
                        AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(Root + "/Content/Relics/Trial_" + PrototypeContentDefaults.TrialKeys[i] + ".asset"));
                    }
                    if (definition.icon == null) definition.icon = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Generated/Items/" + PrototypeContentDefaults.TrialIcons[i] + ".png");
                    EditorUtility.SetDirty(definition);
                }
                for (int id = 3; id <= 6; id++)
                {
                    var definition = relics.Find(r => r.id == id);
                    if (definition != null && definition.icon == null) { definition.icon = auxiliary[id - 3]; EditorUtility.SetDirty(definition); }
                }
                var prism = relics.Find(r => r.id == 2);
                if (prism != null && prism.effect == RelicEffectKind.AmplifyNextAddition && prism.targetRelicId == 0)
                { prism.amplifyAnyAdditive = true; EditorUtility.SetDirty(prism); }
                catalog.relics = relics.ToArray(); EditorUtility.SetDirty(catalog);
                error = catalog.ValidateCatalog(); if (error.Length > 0) throw new InvalidOperationException(error);
                AssetDatabase.SaveAssets(); PrototypeContent.UseCatalog(catalog);
                // Animate static companions too; runtime encounters configure themselves from their definition.
                foreach (var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!PrototypeContent.KnownSpirit(renderer.gameObject.name)) continue;
                    var definition = PrototypeContent.Spirit(renderer.gameObject.name);
                    var animator = renderer.GetComponent<SpiritIdleAnimator>();
                    if (animator == null) animator = Undo.AddComponent<SpiritIdleAnimator>(renderer.gameObject);
                    Undo.RecordObject(animator, "Asignar animacion de espiritu"); animator.Configure(definition);
                    EditorUtility.SetDirty(animator); EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
                }
                Selection.activeObject = catalog;
                EditorUtility.DisplayDialog("Pruebas completadas", "24 fotogramas y cuatro iconos preparados. Reliquias nuevas: " + added +
                    ". Total en catalogo: " + catalog.relics.Length + ". Guarda la escena y prueba mapa, combate y DEBUG. Los datos anteriores se conservan.", "Aceptar");
            }
            catch (Exception error) { Debug.LogException(error); EditorUtility.DisplayDialog("Contenido de prueba", error.Message, "Aceptar"); }
        }
        private static Sprite[] BakeAtlas(string path, int width, int height, int[] xs, int[] ys, string folder, bool perRowScale)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path)) || texture.width != width || texture.height != height)
                    throw new InvalidDataException("Dimensiones inesperadas: " + path);
                var pixels = texture.GetPixels32(); int columns = xs.Length - 1, rows = ys.Length - 1;
                var boxes = new RectInt[columns * rows]; var rowMax = new int[rows]; int max = 0;
                for (int row = 0; row < rows; row++) for (int col = 0; col < columns; col++)
                {
                    var box = Bounds(pixels, width, new RectInt(xs[col], height - ys[row + 1], xs[col + 1] - xs[col], ys[row + 1] - ys[row]));
                    boxes[row * columns + col] = box; rowMax[row] = Math.Max(rowMax[row], Math.Max(box.width, box.height)); max = Math.Max(max, rowMax[row]);
                }
                Directory.CreateDirectory(folder); var paths = new string[boxes.Length];
                for (int i = 0; i < boxes.Length; i++)
                {
                    var box = boxes[i]; float scale = 112f / (perRowScale ? rowMax[i / columns] : max);
                    int w = Math.Max(1, (int)Math.Round(box.width * scale)), h = Math.Max(1, (int)Math.Round(box.height * scale));
                    int left = (128 - w) / 2, bottom = perRowScale ? 8 : (128 - h) / 2;
                    var output = new Color32[128 * 128];
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        int sx = box.x + Math.Min(box.width - 1, (int)((x + .5) * box.width / w));
                        int sy = box.y + Math.Min(box.height - 1, (int)((y + .5) * box.height / h));
                        output[(bottom + y) * 128 + left + x] = pixels[sy * width + sx];
                    }
                    var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                    try { image.SetPixels32(output); image.Apply(); paths[i] = folder + "/Frame_" + i + ".png"; File.WriteAllBytes(paths[i], ImageConversion.EncodeToPNG(image)); }
                    finally { UnityEngine.Object.DestroyImmediate(image); }
                }
                AssetDatabase.Refresh(); var sprites = new Sprite[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("No se encontro el importador de " + paths[i]);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256; importer.isReadable = false;
                    importer.spritePixelsPerUnit = perRowScale ? 112 : 128;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                    if (sprites[i] == null) throw new InvalidOperationException("No se importo " + paths[i]);
                }
                return sprites;
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
        private static RectInt Bounds(Color32[] pixels, int width, RectInt cell)
        {
            int x0 = cell.xMax, y0 = cell.yMax, x1 = -1, y1 = -1;
            for (int y = cell.y; y < cell.yMax; y++) for (int x = cell.x; x < cell.xMax; x++) if (pixels[y * width + x].a > 12)
            { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            if (x1 < x0) throw new InvalidDataException("Celda de sprites vacia.");
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }
        [MenuItem("Tools/Espiritus/10 Animar espiritus y completar pruebas", true)]
        private static bool CanSetup() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
