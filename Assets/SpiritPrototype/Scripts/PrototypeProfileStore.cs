using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SpiritPrototype
{
    public sealed class PrototypeProfileStore
    {
        // Unity inline serialization cannot reliably preserve a null custom class.
        // This flag is authoritative even if Unity expands profile.run to an empty object.
        [Serializable]
        private sealed class ProfileDocument
        {
            public int fileFormat;
            public bool hasRun;
            public PrototypeProfileData profile;
        }

        private readonly string directory;
        public PrototypeProfileStore(string directory) { this.directory = directory; }
        public string PathFor(int slot)
        {
            if (slot < 0 || slot > 2) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "perfil_" + (slot + 1) + ".json");
        }
        private static PrototypeProfileData Decode(string path)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var document = JsonUtility.FromJson<ProfileDocument>(json);
            PrototypeProfileData profile;
            if (document != null && document.fileFormat == 2)
            {
                profile = document.profile;
                if (profile != null && !document.hasRun) profile.run = null;
                if (profile == null || (document.hasRun && profile.run == null))
                    throw new InvalidDataException("Documento de perfil incompleto.");
            }
            else if (document == null || document.fileFormat == 0)
            {
                // Read the previous file format without discarding invalid expeditions.
                profile = JsonUtility.FromJson<PrototypeProfileData>(json);
            }
            else throw new InvalidDataException("Version de archivo de perfil no compatible.");
            if (profile == null || !profile.IsValid()) throw new InvalidDataException("Formato de perfil no compatible o incompleto.");
            return profile;
        }
        public PrototypeProfileData Load(int slot, out string notice, out bool blocked)
        {
            string path = PathFor(slot);
            notice = ""; blocked = false;
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return null;
            try { return Decode(path); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException || error is UnauthorizedAccessException)
            {
                try
                {
                    var backup = Decode(path + ".bak");
                    notice = "Se recupero el respaldo de este perfil.";
                    return backup;
                }
                catch (Exception backupError) when (backupError is IOException || backupError is InvalidDataException || backupError is ArgumentException || backupError is UnauthorizedAccessException)
                {
                    blocked = true;
                    notice = "No se pudo leer este perfil ni su respaldo. Sus archivos se conservaron.";
                    return null;
                }
            }
        }
        public void Save(int slot, PrototypeProfileData profile)
        {
            if (profile == null || !profile.IsValid()) throw new InvalidDataException("El estado del perfil no es valido; no se reemplazo el guardado.");
            Directory.CreateDirectory(directory);
            string path = PathFor(slot), temporary = path + ".tmp";
            var document = new ProfileDocument {fileFormat = 2, hasRun = profile.run != null, profile = profile};
            File.WriteAllText(temporary, JsonUtility.ToJson(document, true), Encoding.UTF8);
            // Verify complete bytes before replacing the current save.
            Decode(temporary);
            if (!File.Exists(path)) { File.Move(temporary, path); return; }
            bool currentValid = true;
            try { Decode(path); }
            catch (Exception error) when (error is IOException || error is InvalidDataException || error is ArgumentException)
            { currentValid = false; }
            if (!currentValid)
            {
                File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                File.Move(temporary, path);
                return;
            }
            try { File.Replace(temporary, path, path + ".bak"); }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, path + ".bak", true);
                File.Copy(temporary, path, true);
                File.Delete(temporary);
            }
        }
    }
}
