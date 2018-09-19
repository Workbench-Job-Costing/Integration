using System;
using System.Collections.Generic;

namespace AppContracts
{
    /// <summary>
    /// Two way communication contract between Workbench and ESRI apps
    /// </summary>
    public class AssetContract
    {
        /// <summary>
        /// From Workbench to ESRI and Back
        /// </summary>
        public int LogHeaderId { get; set; }

        /// <summary>
        /// Selected Asset ID from ESRI
        /// </summary>
        public string AssetId { get; set; }

        /// <summary>
        /// Asset attributes from ESRI
        /// </summary>
        public List<AssetAttributeContract> AssetAttributes { get; set; }
    }

    /// <summary>
    /// Attribute data types
    /// </summary>
    public enum AssetAttributeType
    {
        None,
        Text,
        Number,
        Date
    }

    /// <summary>
    /// List of asset attributes, to be amended in Workbench. 
    /// </summary>
    public class AssetAttributeContract
    {
        public AssetAttributeType AttributeType { get; set; }

        /// <summary>
        /// Attribute name, the mapping has been done on ESRI site. i.e. Diameter 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Value for the attribute: i.e. 100mm
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Contains a list of valid values for a given asset attribute.
        /// </summary>
        public List<AssetAttributeValueContract> ValidValues { get; set; }
    }

    /// <summary>
    /// A Value-Label storage for asset attributes
    /// </summary>
    public class AssetAttributeValueContract
    {
        /// <summary>
        /// Hidden value code i.e. 1, 2, 3
        /// </summary>
        public string Value { get; set; } //1

        /// <summary>
        /// Visible value code i.e. 100mm, 150mm, 200mm
        /// </summary>
        public string Label { get; set; } //100mm
    }
}
