// Unity 6. No third-party packages required. Run from Tools > Espiritus.
// The source atlases are kept intact; the generated PNGs can be replaced later.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpiritPrototype.Editor
{
    public static class SpiritPrototypeTools
    {
        private const string Root = "Assets/SpiritPrototype";
        private const int SourceSize = 1254;

        private sealed class Sheet
        {
            public string File, Folder;
            public int[] X, Y;
            public string[] Names;
            public Sheet(string file, string folder, int[] x, int[] y, string[] names)
            { File = file; Folder = folder; X = x; Y = y; Names = names; }
        }

        // Hand-checked boundaries compensate for spacing in the generated atlases.
        // Y boundaries are measured from the TOP of the original PNG.
        private static readonly Sheet[] Sheets =
        {
            new Sheet("Characters.png", "Characters",
                new[] {0, 418, 836, 1254}, new[] {0, 460, 850, 1254},
                new[] {"Ascua", "Velo", "Raicero", "Campanilla", "Luto", "Farol",
                       "Player_South", "Player_North", "Chest"}),
            new Sheet("Items.png", "Items",
                new[] {0, 330, 640, 940, 1254}, new[] {0, 460, 850, 1254},
                new[] {"CarbonTallado", "LenteVoraz", "MechaErrante", "CampanaCeniza",
                       "PrismaVecino", "SelloAgrietado", "BastionAntiguo", "UltimoFarol",
                       "CuerdaRelevo", "MonedaDuelo", "HealingPotion", "CaptureScroll"}),
            new Sheet("Environment.png", "Environment",
                new[] {0, 418, 836, 1254}, new[] {0, 418, 836, 1254},
                new[] {"FloorPlain", "FloorMoss", "FloorCracked", "Wall", "Door",
                       "Pillar", "Urn", "Torch", "Sanctuary"})
        };

        [MenuItem("Tools/Espiritus/1 Preparar sprites temporales")]
        public static void Prepare()
        {
            int created = 0, skipped = 0;
            var imports = new Dictionary<string, int>();
            try
            {
                foreach (Sheet sheet in Sheets)
                {
                    string source = Root + "/Source/" + sheet.File;
                    if (!File.Exists(source))
                        throw new FileNotFoundException("Copia la carpeta Assets completa del paquete. Falta: " + source);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    try
                    {
                        if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(source)))
                            throw new InvalidDataException("No se pudo leer " + source);
                        if (texture.width != SourceSize || texture.height != SourceSize)
                            throw new InvalidDataException("Se esperaba el atlas original de 1254 x 1254: " + source);
                        Color32[] pixels = texture.GetPixels32();
                        int columns = sheet.X.Length - 1, rows = sheet.Y.Length - 1;
                        string output = Root + "/Generated/" + sheet.Folder;
                        Directory.CreateDirectory(output);
                        for (int row = 0; row < rows; row++)
                        for (int col = 0; col < columns; col++)
                        {
                            string path = output + "/" + sheet.Names[row * columns + col] + ".png";
                            // Rerunning never overwrites an existing generated sprite or its settings.
                            if (File.Exists(path)) { skipped++; continue; }
                            RectInt cell = new RectInt(sheet.X[col], texture.height - sheet.Y[row + 1],
                                sheet.X[col + 1] - sheet.X[col], sheet.Y[row + 1] - sheet.Y[row]);
                            RectInt bounds = VisibleBounds(pixels, texture.width, cell);
                            const int padding = 2;
                            var result = new Texture2D(bounds.width + padding * 2,
                                bounds.height + padding * 2, TextureFormat.RGBA32, false);
                            try
                            {
                                var cropped = new Color32[result.width * result.height];
                                for (int y = 0; y < bounds.height; y++)
                                for (int x = 0; x < bounds.width; x++)
                                    cropped[(y + padding) * result.width + x + padding] =
                                        pixels[(bounds.y + y) * texture.width + bounds.x + x];
                                result.SetPixels32(cropped);
                                result.Apply();
                                File.WriteAllBytes(path, ImageConversion.EncodeToPNG(result));
                                imports.Add(path, Mathf.Max(result.width, result.height));
                                created++;
                            }
                            finally { UnityEngine.Object.DestroyImmediate(result); }
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(texture); }
                }
            }
            catch (Exception error)
            {
                AssetDatabase.Refresh();
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Sprites temporales", error.Message, "Cerrar");
                return;
            }
            AssetDatabase.Refresh();
            foreach (KeyValuePair<string, int> entry in imports)
            {
                string path = entry.Key;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("No hay importador: " + path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.isReadable = false;
                // Normalize the longest edge to one world unit for temporary art.
                importer.spritePixelsPerUnit = entry.Value;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
            Debug.Log("Espiritus: " + created + " sprites creados; " + skipped + " existentes conservados.");
            EditorUtility.DisplayDialog("Sprites preparados",
                created + " nuevos. " + skipped + " existentes conservados.\n" +
                "Estan en Assets/SpiritPrototype/Generated.\n" +
                "Ahora puedes crear la habitacion de muestra desde Tools > Espiritus.", "Aceptar");
        }

        [MenuItem("Tools/Espiritus/1 Preparar sprites temporales", true)]
        private static bool CanPrepare() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

        [MenuItem("Tools/Espiritus/2 Crear habitacion de muestra", true)]
        private static bool CanCreateRoom() { return !EditorApplication.isPlayingOrWillChangePlaymode; }

        private static RectInt VisibleBounds(Color32[] pixels, int stride, RectInt cell)
        {
            int minX = cell.xMax, minY = cell.yMax, maxX = -1, maxY = -1;
            for (int y = cell.yMin; y < cell.yMax; y++)
            for (int x = cell.xMin; x < cell.xMax; x++)
            {
                if (pixels[y * stride + x].a < 32) continue;
                minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
            }
            if (maxX < minX || maxY < minY)
                throw new InvalidDataException("Celda vacia en el atlas: " + cell);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        [MenuItem("Tools/Espiritus/2 Crear habitacion de muestra")]
        public static void CreateRoom()
        {
            // Check dependencies before changing any open scene.
            foreach (Sheet sheet in Sheets)
            foreach (string name in sheet.Names)
                if (Load(sheet.Folder, name) == null)
                {
                    EditorUtility.DisplayDialog("Faltan sprites", "Ejecuta primero Tools > Espiritus > 1 Preparar sprites temporales.", "Aceptar");
                    return;
                }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Habitacion temporal");
            Transform floor = new GameObject("Suelo").transform; floor.SetParent(root.transform);
            Transform walls = new GameObject("Muros con colision").transform; walls.SetParent(root.transform);
            Transform props = new GameObject("Objetos").transform; props.SetParent(root.transform);
            Transform actors = new GameObject("Personajes estaticos").transform; actors.SetParent(root.transform);
            string[] floors = {"FloorPlain", "FloorMoss", "FloorCracked"};
            for (int y = -4; y <= 3; y++)
            for (int x = -6; x <= 5; x++)
                Place("Environment", floors[((x + 6) * 3 + (y + 4) * 5) % 3], x, y, 1.01f, 1.01f, -10, floor);
            for (int y = -5; y <= 4; y++)
            for (int x = -7; x <= 6; x++)
            {
                if (x != -7 && x != 6 && y != -5 && y != 4) continue;
                GameObject wall = Place("Environment", x == 0 && y == 4 ? "Door" : "Wall",
                    x, y, 1.01f, 1.01f, 0, walls);
                wall.AddComponent<BoxCollider2D>().size = wall.GetComponent<SpriteRenderer>().sprite.bounds.size;
            }
            Place("Environment", "Sanctuary", -4, 1, 1.3f, 1.3f, -5, props);
            Place("Environment", "Pillar", 0, 1, .9f, .9f, 1, props);
            Place("Environment", "Urn", -5, -2, .55f, .55f, 1, props);
            Place("Environment", "Torch", -6, 3, .45f, .8f, 1, props);
            Place("Environment", "Torch", 5, 3, .45f, .8f, 1, props);
            Place("Characters", "Chest", 4, 2, .85f, .7f, 1, props);
            Place("Characters", "Player_South", -2, -2, .65f, .95f, 10, actors);
            Place("Characters", "Ascua", -2, -3, .65f, .8f, 10, actors);
            Place("Characters", "Velo", 2, 1, .85f, .95f, 10, actors);
            Place("Characters", "Raicero", 4, 0, .85f, .95f, 10, actors);
            Place("Characters", "Campanilla", -4, 2.5f, .8f, .75f, 10, actors);
            Place("Characters", "Luto", 1.5f, -2, .8f, .95f, 10, actors);
            Place("Characters", "Farol", 4, -2.5f, .85f, .85f, 10, actors);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 6.4f;
            camera.transform.position = new Vector3(-.5f, -.5f, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .04f, .065f);
            // Unlit materials keep this preview visible in both Built-in and URP 2D.
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            string path = AssetDatabase.GenerateUniqueAssetPath(Root + "/Scenes/HabitacionTemporal.unity");
            EditorSceneManager.SaveScene(scene, path);
            Selection.activeGameObject = root;
            Debug.Log("Habitacion visual creada en " + path + ". Sin movimiento ni combate todavia.");
        }

        private static Sprite Load(string folder, string name)
        { return AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Generated/" + folder + "/" + name + ".png"); }

        private static GameObject Place(string folder, string name, float x, float y,
            float width, float height, int order, Transform parent)
        {
            Sprite sprite = Load(folder, name);
            var go = new GameObject(name); go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0);
            go.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            renderer.sharedMaterial = GetUnlitMaterial();
            return go;
        }

        private static Material GetUnlitMaterial()
        {
            const string path = Root + "/Materials/PrototypeUnlit.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") : Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("No se encontro un shader de sprites. Usa un proyecto 2D Built-in o Universal 2D.");
            Directory.CreateDirectory(Root + "/Materials");
            AssetDatabase.Refresh();
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
