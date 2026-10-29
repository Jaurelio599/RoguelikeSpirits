using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SpiritPrototype.Editor
{
    public static class PlayerAnimationSetup
    {
        private const string Root = "Assets/SpiritPrototype";
        private const string Sheet = Root + "/Source/PlayerWalkSheet_v01.png";
        [MenuItem("Tools/Espiritus/9 Preparar animacion del jugador")]
        private static void Setup()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (players.Length == 0) { EditorUtility.DisplayDialog("Abre la habitacion", "Abre HabitacionTemporal con el refugio y el personaje que ya funcionan.", "Aceptar"); return; }
            if (!File.Exists(Sheet)) { EditorUtility.DisplayDialog("Falta la hoja", "Copia tambien Source/PlayerWalkSheet_v01.png de este paquete.", "Aceptar"); return; }
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(source, File.ReadAllBytes(Sheet))) throw new InvalidDataException("No se pudo leer la hoja del jugador.");
                var pixels = source.GetPixels32();
                var frames = PlayerSpriteBaker.Analyze(pixels, source.width, source.height, out float scale);
                string folder = Root + "/Generated/PlayerWalk";
                Directory.CreateDirectory(folder); Directory.CreateDirectory(Root + "/Animations");
                string[] directions = {"South", "North", "West", "East"};
                var paths = new string[16];
                for (int row = 0; row < 4; row++)
                for (int col = 0; col < 4; col++)
                {
                    int index = row * 4 + col;
                    paths[index] = folder + "/Player_" + directions[row] + "_" + col + ".png";
                    var image = new Texture2D(PlayerSpriteBaker.Canvas, PlayerSpriteBaker.Canvas, TextureFormat.RGBA32, false);
                    try
                    {
                        image.SetPixels32(PlayerSpriteBaker.Bake(pixels, source.width, frames[index], scale)); image.Apply();
                        File.WriteAllBytes(paths[index], ImageConversion.EncodeToPNG(image));
                    }
                    finally { UnityEngine.Object.DestroyImmediate(image); }
                }
                AssetDatabase.Refresh();
                var sprites = new Sprite[16];
                for (int i = 0; i < paths.Length; i++)
                {
                    var importer = AssetImporter.GetAtPath(paths[i]) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("No hay importador para " + paths[i]);
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                    importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 256;
                    importer.isReadable = false; importer.spritePixelsPerUnit = PlayerSpriteBaker.BodyPixels;
                    var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.Center; settings.spritePivot = new Vector2(.5f, .5f);
                    settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
                    sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
                    if (sprites[i] == null) throw new InvalidOperationException("No se importo el sprite " + paths[i]);
                }
                string assetPath = Root + "/Animations/PlayerWalkSet.asset";
                var data = AssetDatabase.LoadAssetAtPath<PlayerWalkSet>(assetPath);
                if (data == null) { data = ScriptableObject.CreateInstance<PlayerWalkSet>(); AssetDatabase.CreateAsset(data, assetPath); }
                Undo.RecordObject(data, "Configurar fotogramas del jugador");
                data.south = Slice(sprites, 0); data.north = Slice(sprites, 4); data.west = Slice(sprites, 8); data.east = Slice(sprites, 12);
                EditorUtility.SetDirty(data); AssetDatabase.SaveAssets();
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Activar animacion del jugador");
                foreach (var player in players)
                {
                    var animator = player.GetComponent<PlayerSpriteAnimator>();
                    if (animator == null) animator = Undo.AddComponent<PlayerSpriteAnimator>(player.gameObject);
                    Undo.RecordObject(animator, "Asignar fotogramas"); animator.Configure(data);
                    EditorUtility.SetDirty(animator); EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
                }
                Undo.CollapseUndoOperations(group); Selection.activeObject = data;
                EditorUtility.DisplayDialog("Animacion preparada", "16 fotogramas alineados. Se configuraron " + players.Length +
                    " personajes, incluidos los inactivos del refugio. Guarda la escena con Ctrl+S y pulsa Play.", "Aceptar");
            }
            catch (Exception error) { Debug.LogException(error); EditorUtility.DisplayDialog("Animacion del jugador", error.Message, "Aceptar"); }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }
        private static Sprite[] Slice(Sprite[] sprites, int offset)
        { return new[] {sprites[offset], sprites[offset + 1], sprites[offset + 2], sprites[offset + 3]}; }
        [MenuItem("Tools/Espiritus/9 Preparar animacion del jugador", true)]
        private static bool CanSetup() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
