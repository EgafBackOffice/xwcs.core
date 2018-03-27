﻿using System;
using DevExpress.XtraDataLayout;
using DevExpress.XtraEditors;

namespace xwcs.core.db.binding.attributes
{
	[AttributeUsage(AttributeTargets.Class,	AllowMultiple = true)]
	public class StaticMetatypeAttribute : CustomAttribute
	{
        public Type MetadataClassType { get; set; }

        

    }	
}