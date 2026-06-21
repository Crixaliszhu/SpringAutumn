using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using SpringAutumn.Config;
using SpringAutumn.Runtime;

namespace SpringAutumn.Save
{
    public interface ISaveStorage
    {
        bool Exists(int slot);
        string Read(int slot);
        void Write(int slot, string json);
        void Delete(int slot);
    }

    public class FileSaveStorage : ISaveStorage
    {
        private readonly string _directory;

        public FileSaveStorage(string directory)
        {
            _directory = directory;
            Directory.CreateDirectory(_directory);
        }

        public bool Exists(int slot) => File.Exists(PathFor(slot));
        public string Read(int slot) => File.ReadAllText(PathFor(slot), Encoding.UTF8);
        public void Write(int slot, string json) => File.WriteAllText(PathFor(slot), json, Encoding.UTF8);
        public void Delete(int slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path))
                File.Delete(path);
        }

        private string PathFor(int slot) => Path.Combine(_directory, "save_" + slot + ".json");
    }

    public class SaveJsonSerializer
    {
        public string Serialize(SaveData data)
        {
            var serializer = new DataContractJsonSerializer(typeof(SaveData));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, data);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public SaveData Deserialize(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(SaveData));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (SaveData)serializer.ReadObject(stream);
            }
        }
    }

    public class SaveManager
    {
        public const int SlotCount = 3;

        private readonly ConfigDatabase _config;
        private readonly ISaveStorage _storage;
        private readonly SaveConverter _converter;
        private readonly SaveJsonSerializer _serializer;

        public string LastError { get; private set; }

        public SaveManager(ConfigDatabase config, ISaveStorage storage)
        {
            _config = config;
            _storage = storage;
            _converter = new SaveConverter();
            _serializer = new SaveJsonSerializer();
        }

        public bool Save(WorldRuntime world, int slot)
        {
            if (!IsValidSlot(slot))
            {
                LastError = "非法存档槽: " + slot;
                return false;
            }

            try
            {
                var data = _converter.ToSave(world, slot);
                _storage.Write(slot, _serializer.Serialize(data));
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public WorldRuntime Load(int slot)
        {
            if (!IsValidSlot(slot))
            {
                LastError = "非法存档槽: " + slot;
                return null;
            }

            try
            {
                if (!_storage.Exists(slot))
                {
                    LastError = "存档不存在: " + slot;
                    return null;
                }

                var data = _serializer.Deserialize(_storage.Read(slot));
                LastError = null;
                return _converter.Restore(data, _config);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return null;
            }
        }

        public void Delete(int slot)
        {
            if (IsValidSlot(slot))
                _storage.Delete(slot);
        }

        public List<SaveInfo> GetSaveList()
        {
            var list = new List<SaveInfo>();
            for (int slot = 1; slot <= SlotCount; slot++)
            {
                if (!_storage.Exists(slot))
                    continue;

                try
                {
                    var data = _serializer.Deserialize(_storage.Read(slot));
                    list.Add(data.info);
                }
                catch
                {
                    list.Add(new SaveInfo { slot = slot, displayName = "损坏存档" });
                }
            }
            return list;
        }

        private static bool IsValidSlot(int slot) => slot >= 1 && slot <= SlotCount;
    }
}
