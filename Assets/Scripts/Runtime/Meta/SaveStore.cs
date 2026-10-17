using System;
using System.IO;
using UnityEngine;

namespace Wickfall.Meta
{
    public enum SaveLoadResult { NoSave, Loaded, RestoredFromBackup, Corrupt }

    /// <summary>
    /// Reads and writes the save files (G25): an encrypted save, a backup of the previous good save,
    /// and a plain settings file. A damaged save falls back to the backup, so a crash never bricks a run.
    /// </summary>
    public sealed class SaveStore
    {
        public const string SaveFileName = "wickfall.sav";
        public const string BackupFileName = "wickfall.sav.bak";
        public const string SettingsFileName = "settings.json";

        public string Directory { get; }
        public string SavePath => Path.Combine(Directory, SaveFileName);
        public string BackupPath => Path.Combine(Directory, BackupFileName);
        public string SettingsPath => Path.Combine(Directory, SettingsFileName);

        public SaveStore(string directory)
        {
            Directory = directory;
        }

        public static SaveStore Default => new SaveStore(Application.persistentDataPath);

        public bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        public void Write(SaveData data)
        {
            System.IO.Directory.CreateDirectory(Directory);
            byte[] bytes = SaveCodec.EncodeSave(data);

            // Keep the last good save as the backup before replacing it.
            if (File.Exists(SavePath) && SaveCodec.TryDecode(File.ReadAllBytes(SavePath), out _))
                File.Copy(SavePath, BackupPath, true);

            string temp = SavePath + ".tmp";
            File.WriteAllBytes(temp, bytes);
            if (File.Exists(SavePath))
                File.Delete(SavePath);
            File.Move(temp, SavePath);
        }

        public SaveLoadResult TryLoad(out SaveData data)
        {
            data = null;
            bool hasPrimary = File.Exists(SavePath);
            bool hasBackup = File.Exists(BackupPath);
            if (!hasPrimary && !hasBackup)
                return SaveLoadResult.NoSave;

            if (hasPrimary && SaveCodec.TryDecodeSave(ReadSafe(SavePath), out data))
                return SaveLoadResult.Loaded;

            if (hasBackup && SaveCodec.TryDecodeSave(ReadSafe(BackupPath), out data))
            {
                Debug.LogWarning("[WICKFALL Save] The save was damaged; restored the backup copy.");
                try
                {
                    File.Copy(BackupPath, SavePath, true);
                }
                catch (IOException)
                {
                    // The game still runs from the backup in memory.
                }
                return SaveLoadResult.RestoredFromBackup;
            }

            data = null;
            return SaveLoadResult.Corrupt;
        }

        public void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
        }

        public SettingsData LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var settings = JsonUtility.FromJson<SettingsData>(File.ReadAllText(SettingsPath));
                    if (settings != null)
                        return settings;
                }
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                Debug.LogWarning("[WICKFALL Save] Settings unreadable, using defaults: " + e.Message);
            }
            return new SettingsData();
        }

        public void SaveSettings(SettingsData settings)
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, true));
        }

        static byte[] ReadSafe(string path)
        {
            try
            {
                return File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }
}
