using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace PSAttack.Shell
{
    public class PSParameter
    {
        [Category("Parameters"), Browsable(true), ReadOnly(false), Bindable(false), DesignOnly(false)]
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public object Value { get; set; }
        public object DefaultValue { get; set; }
        public bool IsFileName { get; set; }
        public bool IsHostList { get; set; }
        public bool IsCredential { get; set; }
        private Type type;

        [XmlIgnoreAttribute()]
        public Type Type
        {
            get { return type; }
            set { type = value; }
        }

        public string TypeName
        {
            get
            {
                return ((null != Value) || (null != DefaultValue))
                    ? (Value ?? DefaultValue).GetType().ToString()
                    : string.Empty;
            }
            set { type = Type.GetType(value); }
        }
    }
}
