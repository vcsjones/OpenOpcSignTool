using System;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenVsixSignTool
{
    public class OpcPackage : IDisposable
    {
        private const string CONTENT_TYPES_XML = "[Content_Types].xml";


        private readonly ZipArchive _archive;
        private readonly OpcPackageFileMode _mode;
        private bool _disposed = true;
        private OpcContentTypes _contentTypes;

        public static OpcPackage Open(string path, OpcPackageFileMode mode = OpcPackageFileMode.Read)
        {
            var zipMode = GetZipModeFromOpcPackageMode(mode);
            var zip = ZipFile.Open(path, zipMode);
            return new OpcPackage(zip, mode);
        }

        private OpcPackage(ZipArchive archive, OpcPackageFileMode mode)
        {
            _disposed = false;
            _archive = archive;
            _mode = mode;
        }

        public OpcContentTypes ContentTypes
        {
            get
            {
                if (_contentTypes == null)
                {
                    _contentTypes = ConstructContentTypes();
                }
                return _contentTypes;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _archive.Dispose();
            }
        }

        public void Save()
        {
            if (_contentTypes?.IsDirty == true)
            {
                var entry = _archive.GetEntry(CONTENT_TYPES_XML) ?? _archive.CreateEntry(CONTENT_TYPES_XML);
                using (var stream = entry.Open())
                {
                    var newXml = _contentTypes.ToXml();
                    newXml.Save(stream, SaveOptions.None);
                    _contentTypes.IsDirty = false;
                }
            }
        }

        private OpcContentTypes ConstructContentTypes()
        {
            var entry = _archive.GetEntry(CONTENT_TYPES_XML);
            var readOnlyMode = _mode != OpcPackageFileMode.ReadWrite;
            if (entry == null)
            {
                return new OpcContentTypes(readOnlyMode);
            }
            else
            {
                using (var stream = entry.Open())
                {
                    return new OpcContentTypes(XDocument.Load(stream, LoadOptions.PreserveWhitespace), readOnlyMode);
                }
            }
        }

        private static ZipArchiveMode GetZipModeFromOpcPackageMode(OpcPackageFileMode mode)
        {
            switch (mode)
            {
                case OpcPackageFileMode.Read:
                    return ZipArchiveMode.Read;
                case OpcPackageFileMode.ReadWrite:
                    return ZipArchiveMode.Update;
                default:
                    throw new ArgumentException($"Specified {nameof(OpcPackageFileMode)} is invalid.", nameof(mode));
            }
        }
    }
}
