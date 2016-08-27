﻿using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace PSAttack.Shell
{
    [TypeConverter(typeof(PSParamType.PSParamObject))]
    public class PSParamType
    {
        [Category("Standard")]
        private readonly List<PSParameter> psparams = new List<PSParameter>();

        [Browsable(false)]
        public List<PSParameter> Properties
        {
            get { return psparams; }
        }

        private Dictionary<string, object> values = new Dictionary<string, object>();

        public object this[String name]
        {
            get
            {
                object val;
                values.TryGetValue(name, out val);
                return val;
            }
            set { values.Remove(name); }
        }

        private class PSParamObject : ExpandableObjectConverter
        {
            public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            {
                PropertyDescriptorCollection props = base.GetProperties(context, value, attributes);
                PSParamType parm = value as PSParamType;
                List<PSParameter> psprops = null;
                int propcnt = props.Count;
                if (null != parm) {
                    psprops = parm.Properties;
                    propcnt += psprops.Count;
                }
                PropertyDescriptor[] psdescs = new PropertyDescriptor[propcnt];
                props.CopyTo(psdescs, 0);

                if (null != psprops) {
                    int idx = props.Count;
                    foreach (PSParameter psparam in psprops) {
                        psdescs[idx++] = new PSParamDescriptor(psparam);
                    }
                }
                return new PropertyDescriptorCollection(psdescs);
            }
        }

        private class PSParamDescriptor : PropertyDescriptor
        {
            private readonly PSParameter psparam;

            public PSParamDescriptor(PSParameter psparam)
                : base(psparam.Name, null)
            {
                this.psparam = psparam;
            }

            public override string Category
            {
                get { return psparam.Category; }
            }

            public override string Description
            {
                get { return psparam.Description; }
            }

            public override string Name
            {
                get { return psparam.Name; }
            }

            public override bool ShouldSerializeValue(object component)
            {
                return ((PSParamType)component)[psparam.Name] != null;
            }

            public override void ResetValue(object component)
            {
                ((PSParamType)component)[psparam.Name] = null;
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override Type PropertyType
            {
                get { return psparam.Type; }
            }

            public override bool CanResetValue(object component)
            {
                return true;
            }

            public override Type ComponentType
            {
                get { return typeof(PSParamType); }
            }

            public override void SetValue(object component, object value)
            {
                psparam.Value = value;
            }

            public override object GetValue(object component)
            {
                return psparam.Value ?? psparam.DefaultValue;
            }
        }
    }
}