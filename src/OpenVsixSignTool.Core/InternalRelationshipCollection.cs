using System.Collections.Generic;
using System.Xml;

namespace OpenVsixSignTool.Core
{
    internal static class InternalRelationshipCollection
    {
        /// <summary>
        /// Write one Relationship element for each member of relationships.
        /// This method is used by XmlDigitalSignatureProcessor code as well
        /// </summary>
        internal static void WriteRelationshipsAsXml(XmlWriter writer, List<OpcRelationship> relationships, bool alwaysWriteTargetModeAttribute, bool inStreamingProduction)
        {
            foreach (OpcRelationship relationship in relationships)
            {
                writer.WriteStartElement(RelationshipTagName);

                // Write RelationshipType attribute.
                writer.WriteAttributeString(TypeAttributeName, relationship.Type.ToString());

                // Write Target attribute.
                // We would like to persist the uri as passed in by the user and so we use the
                // OriginalString property. This makes the persisting behavior consistent
                // for relative and absolute Uris. 
                // Since we accpeted the Uri as a string, we are at the minimum guaranteed that
                // the string can be converted to a valid Uri. 
                // Also, we are just using it here to persist the information and we are not
                // resolving or fetching a resource based on this Uri.
                writer.WriteAttributeString(TargetAttributeName, relationship.Target.OriginalString);

                // TargetMode is optional attribute in the markup and its default value is TargetMode="Internal" 
                if (alwaysWriteTargetModeAttribute)
                    writer.WriteAttributeString(TargetModeAttributeName, "Internal");

                // Write Id attribute.
                writer.WriteAttributeString(IdAttributeName, relationship.Id);

                writer.WriteEndElement();
            }
        }

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private static readonly string RelationshipTagName = "Relationship";
        private static readonly string TargetAttributeName = "Target";
        private static readonly string TypeAttributeName = "Type";
        private static readonly string IdAttributeName = "Id";
        private static readonly string TargetModeAttributeName = "TargetMode";
    }
}
