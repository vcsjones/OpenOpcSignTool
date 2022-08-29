using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    internal class OpcSignatureManifest
    {
        private readonly List<OpcPartDigest> _digests;

        private OpcSignatureManifest(List<OpcPartDigest> digests)
        {
            digests.Sort(delegate (OpcPartDigest x, OpcPartDigest y)
            {
                if (x.ReferenceUri == null && y.ReferenceUri == null) return 0;
                else if (x.ReferenceUri == null) return -1;
                else if (y.ReferenceUri == null) return 1;
                else return String.Compare(x.ReferenceUri.ToString(), y.ReferenceUri.ToString(), comparisonType: StringComparison.OrdinalIgnoreCase); ;
            });

            _digests = digests;
        }  

        /// <summary>
        /// Synthesizes a NodeList of Reference tags to hash
        /// </summary>
        /// <param name="relationships"></param>
        /// <returns></returns>
        internal static Stream GenerateRelationshipNodeStream(List<OpcRelationship> relationships)
        {
            // create a NodeList containing valid Relationship XML and serialize it to the stream
            Stream s = new MemoryStream();

            // Wrap in a stream that ignores Flush and Close so the XmlTextWriter
            // will not close it.
            // use UTF-8 encoding by default
            using (XmlTextWriter writer = new XmlTextWriter(new IgnoreFlushAndCloseStream(s),
                System.Text.Encoding.UTF8))
            {
                // start outer Relationships tag
                writer.WriteStartElement(XTable.Get(XTable.ID.RelationshipsTagName), "http://schemas.openxmlformats.org/package/2006/relationships");

                // generate a valid Relationship tag according to the Opc schema
                InternalRelationshipCollection.WriteRelationshipsAsXml(writer, relationships,
                        true,  /* systematically write target mode */
                        false  /* not in streaming production */
                        );

                // end of Relationships tag
                writer.WriteEndElement();
            }
            s.Position = 0;
            return s;
        }

        //Returns the sorted OpcRelationship collection
        private static List<OpcRelationship> GetRelationships(
            OpcPart part)
        {
            SortedDictionary<String, OpcRelationship>
                relationshipsDictionarySortedById = new SortedDictionary<String, OpcRelationship>(StringComparer.Ordinal);

            //foreach (PackageRelationshipSelector relationshipSelector in relationshipSelectorsWithSameSource)
            {
                // loop and accumulate and group them by owning Part
                foreach (OpcRelationship r in part.Package.Relationships)
                {
                    // add relationship
                    if (!relationshipsDictionarySortedById.ContainsKey(r.Id))
                        relationshipsDictionarySortedById.Add(r.Id, r);
                }
            }
            List<OpcRelationship> rels = new List<OpcRelationship>();
            int count = 0;
            foreach (OpcRelationship rel in relationshipsDictionarySortedById.Values)
            {
                // Since we don´t have the PackageRelationshipSelector, so we remove the origin relationship here
                if (rel.Target.ToString().Equals(XTable.Get(XTable.ID.OriginFileUri))) 
                {
                    continue;
                }
                rels.Insert(count, rel);
                count++;
            }
            return rels;
        }

        public static (OpcSignatureManifest, XmlNodeList) Build(ISigningContext context, HashSet<OpcPart> parts)
        {
            XmlDocument dummyDocument = new XmlDocument();
            XmlNodeList nodes = dummyDocument.SelectNodes("/*/*");
            var digests = new List<OpcPartDigest>(parts.Count);
            foreach (var part in parts)
            {
                if (part.Entry.ToString().Equals("_rels/.rels"))
                {
                    {
                        var transformer = new XmlDsigC14NTransform(false);
                        transformer.LoadInput(part.Entry.Open());
                        var result = transformer.GetOutput(typeof(Stream));
                        if (result is Stream s)
                        {
                            var (digest, identifier) = OpcPartDigestProcessor.Digest(s, context.FileDigestAlgorithmName);
                            var builder = new UriBuilder(part.Uri)
                            {
                                Query = "ContentType=" + part.ContentType
                            };

                            digests.Add(new OpcPartDigest(builder.Uri, identifier, digest));
                            s.Close();
                        }
                    }
                    {
                        Stream relNode = GenerateRelationshipNodeStream(GetRelationships(part));

                        XmlDocument newDocument = new XmlDocument();
                        newDocument.Load(relNode);
                        nodes = newDocument.SelectNodes("/*/*");

                        relNode.Position = 0;

                        var transformer = new XmlDsigC14NTransform();
                        transformer.LoadInput(relNode);
                        var result = transformer.GetOutput(typeof(Stream));
                        if (result is Stream s)
                        {
                            var (digest, identifier) = OpcPartDigestProcessor.Digest(s, context.FileDigestAlgorithmName);
                            var builder = new UriBuilder(part.Uri)
                            {
                                Query = "ContentType=" + part.ContentType
                            };

                            digests.Add(new OpcPartDigest(builder.Uri, identifier, digest));
                            s.Close();
                        }
                    }
                }
                else
                {
                    var (digest, identifier) = OpcPartDigestProcessor.Digest(part, context.FileDigestAlgorithmName);
                    var builder = new UriBuilder(part.Uri)
                    {
                        Query = "ContentType=" + part.ContentType
                    };

                    digests.Add(new OpcPartDigest(builder.Uri, identifier, digest));
                }
            }
            return (new OpcSignatureManifest(digests), nodes);
        }

        public IReadOnlyList<OpcPartDigest> Manifest => _digests;
    }
}
