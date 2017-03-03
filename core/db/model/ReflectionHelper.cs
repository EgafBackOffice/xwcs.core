using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Linq;
using System.Text;
using xwcs.core.db.binding.attributes;
using System.Reflection.Emit;

namespace xwcs.core.db.model{

	public static class ExtensionMethods
	{
		public static bool TryGetInterfaceGenericParameters(this Type type, Type @interface, out Type[] typeParameters)
		{
			typeParameters = null;

			if (type.IsGenericType && type.GetGenericTypeDefinition() == @interface)
			{
				typeParameters = type.GetGenericArguments();
				return true;
			}

			var implements = type.FindInterfaces((ty, obj) => ty.IsGenericType && ty.GetGenericTypeDefinition() == @interface, null).FirstOrDefault();
			if (implements == null)
				return false;

			typeParameters = implements.GetGenericArguments();
			return true;
		}


		public static Y MapTo<T, Y>(this T input) where Y : class, new()
		{
			Y output = new Y();
			var propsT = typeof(T).GetProperties();
			var propsY = typeof(Y).GetProperties();

			var similarsT = propsT.Where(x =>
						  propsY.Any(y => y.Name == x.Name
				   && y.PropertyType == x.PropertyType)).OrderBy(x => x.Name).ToList();

			var similarsY = propsY.Where(x =>
							propsT.Any(y => y.Name == x.Name
					&& y.PropertyType == x.PropertyType)).OrderBy(x => x.Name).ToList();

			for (int i = 0; i < similarsY.Count; i++)
			{
				similarsY[i]
				.SetValue(output, similarsT[i].GetValue(input, null), null);
			}

			return output;
		}

		public static void CopyTo<T>(this T input, ref T output)
		{
			foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(input))
			{
				pd.SetValue(output, pd.GetValue(input));
			}
		}

		public static void CopyFrom(this object dest, object input)
		{
			foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(dest))
			{
				pd.SetValue(dest, pd.GetValue(input));
			}
		}

		public static object GetPropValueByPathUsingReflection(this object obj, string name)
		{
			foreach (string part in name.Split('.'))
			{
				if (obj == null) { return null; }
				System.Reflection.PropertyInfo info = obj.GetType().GetProperty(part);
				if (info == null) { return null; }

				obj = info.GetValue(obj, null);
			}
			return obj;
		}

		public static void SetPropValueByPathUsingReflection(this object obj, string name, object value)
		{
			try
			{
				object lastObject = null;

				System.Reflection.PropertyInfo info = null;
				foreach (string part in name.Split('.'))
				{
					if (obj == null) { return; }
					//get info of property connected to current obj
					info = obj.GetType().GetProperty(part);
					if (info == null) { return; }
					//go deeper
					lastObject = obj;
					obj = info.GetValue(obj, null);
				}
				//we are at the end so set value
				info.SetValue(lastObject, value, null);
			}
			catch (Exception)
			{
				throw new InvalidEnumArgumentException();
			}
		}

		public static T CreateDelegate<T>(this MethodInfo method) where T : class
		{
			return Delegate.CreateDelegate(typeof(T), method) as T;
		}


        /// <summary>
        /// Alternative version of <see cref="Type.IsSubclassOf"/> that supports raw generic types (generic types without
        /// any type parameters).
        /// </summary>
        /// <param name="baseType">The base type class for which the check is made.</param>
        /// <param name="toCheck">To type to determine for whether it derives from <paramref name="baseType"/>.</param>
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type baseType)
        {
            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public static bool IsSubclassOfRawGeneric(this FieldInfo toCheckFo, Type baseType)
        {
            Type toCheck = toCheckFo.FieldType;

            while (toCheck != typeof(object))
            {
                Type cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (baseType == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

    }

	

	/// <summary>
	/// Contains helper functions related to reflection
	/// </summary>
	public static class ReflectionHelper
    {
        /// <summary>
        /// Searches for a property in the given property path
        /// </summary>
        /// <param name="rootType">The root/starting point to start searching</param>
        /// <param name="propertyPath">The path to the property. Ex Customer.Address.City</param>
        /// <returns>A <see cref="PropertyInfo"/> describing the property in the property path.</returns>
        public static PropertyInfo GetPropertyFromPath(Type rootType,string propertyPath)
        {
            if (rootType == null)
                throw new ArgumentNullException("rootType");
            
            Type propertyType = rootType;
            PropertyInfo propertyInfo = null;
            string[] pathElements = propertyPath.Split(new char[1] { '.' });
            for (int i = 0; i < pathElements.Length; i++)
            {
                propertyInfo = propertyType.GetProperty(pathElements[i], BindingFlags.Public | BindingFlags.Instance);
                if (propertyInfo != null)
                {
                    propertyType = propertyInfo.PropertyType;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("propertyPath",propertyPath,"Invalid property path");
                }
            }
            return propertyInfo;
        }


        public static PropertyDescriptor GetPropertyDescriptorFromPath(Type rootType, string propertyPath)
        {
            string propertyName;
            bool lastProperty = false;
            if (rootType == null)
                throw new ArgumentNullException("rootType");

            if (propertyPath.Contains("."))
                propertyName = propertyPath.Substring(0, propertyPath.IndexOf("."));
            else
            {
                propertyName = propertyPath;
                lastProperty = true;
            }

            PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(rootType)[propertyName];
            if (propertyDescriptor == null)
                throw new ArgumentOutOfRangeException("propertyPath", propertyPath, string.Format("Invalid property path for type '{0}' ",rootType.Name));


            if (!lastProperty)
                return GetPropertyDescriptorFromPath(propertyDescriptor.PropertyType, propertyPath.Substring(propertyPath.IndexOf(".") + 1));
            else
                return propertyDescriptor;
        }

		public static IEnumerable<CustomAttribute> GetCustomAttributesFromPath(Type rootType, string propertyPath) {
			PropertyDescriptor pd = GetPropertyDescriptorFromPath(rootType, propertyPath);
			Type t1 = pd.ComponentType;
			IEnumerable<CustomAttribute> attrs1 = t1.GetProperty(pd.Name).GetCustomAttributes(typeof(CustomAttribute), true).Cast<CustomAttribute>();
			List<MetadataTypeAttribute> l = TypeDescriptor.GetAttributes(t1).OfType<MetadataTypeAttribute>().ToList();
			if(l.Count > 0) {
				Type t2 = l.Single().MetadataClassType;
				PropertyInfo pi = t2.GetProperty(pd.Name);
				if(pi != null) {
					IEnumerable<CustomAttribute> attrs2 = pi.GetCustomAttributes(typeof(CustomAttribute), true).Cast<CustomAttribute>();
					return attrs1.Union(attrs2);
				}
			}
			return attrs1;
		}

		public static void CopyObject(object from, object to) {
			to.CopyFrom(from);
		}


        public static PropertyBuilder AddProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            const MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig;

            FieldBuilder field = typeBuilder.DefineField("_" + propertyName, typeof(string), FieldAttributes.Private);
            PropertyBuilder property = typeBuilder.DefineProperty(propertyName, PropertyAttributes.None, propertyType,
                new[] { propertyType });

            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_value", getSetAttr, propertyType,
                Type.EmptyTypes);
            ILGenerator getIl = getMethodBuilder.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, field);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_value", getSetAttr, null,
                new[] { propertyType });
            ILGenerator setIl = setMethodBuilder.GetILGenerator();
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, field);
            setIl.Emit(OpCodes.Ret);

            property.SetGetMethod(getMethodBuilder);
            property.SetSetMethod(setMethodBuilder);

            return property;
        }
    }
}
