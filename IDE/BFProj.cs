using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDE
{
    [Serializable]
    public class BFProj
    {
        public class TextFile
        {
            public bool modified { get; set; } = false;
            public string text { get; set; } = "";
        }

        public string StartingFile { get; }

        [JsonIgnore]
        public string? CurrentFile { get; set; } = null;

        [JsonIgnore]
        public Dictionary<string, TextFile> Files { get; } = new();

        public BFProj(string startingFile)
        {
            StartingFile = startingFile;
        }

        public void Save(string path)
        {
            using (StreamWriter w = new StreamWriter(path))
            {
                w.Write(JsonSerializer.Serialize(this));
            }
        }

        public static BFProj? Parse(string path)
        {
            using (StreamReader r = new StreamReader(path))
            {
                string json = r.ReadToEnd();
                return JsonSerializer.Deserialize<BFProj>(json);
            }
        }

        public void SaveFile()
        {
            if (CurrentFile == null)
                return;
            using StreamWriter sw = File.CreateText(CurrentFile);
            sw.Write(Files[CurrentFile].text);
            Files[CurrentFile].modified = false;
        }

        public void SaveAllFile()
        {
            foreach (var file in Files)
            {
                if (file.Value.modified)
                {
                    using StreamWriter sw = File.CreateText(file.Key);
                    sw.Write(file.Value.text);
                    file.Value.modified = false;
                }
            }
        }
    }
}
