using System;
using System.Collections.Generic;

namespace AMU.AssetManager.Data
{
    [Serializable]
    public class BPMFileInfo
    {
        public string fileName;
        public string downloadLink;
    }

    [Serializable]
    public class BPMPackage
    {
        public string packageName;
        public string itemUrl;
        public string imageUrl;
        public List<BPMFileInfo> files;
    }

    [Serializable]
    public class BPMLibrary
    {
        public string lastUpdated;
        public Dictionary<string, List<BPMPackage>> authors;
    }
}
