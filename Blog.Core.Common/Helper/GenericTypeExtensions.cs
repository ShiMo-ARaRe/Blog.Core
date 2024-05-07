using System;
using System.Linq;

namespace Blog.Core.Common.Helper
{
    /// <summary>
    /// 扩展了与泛型类型相关的方法
    /// </summary>
    public static class GenericTypeExtensions
    {
        /// <summary>
        /// 判断类型是否实现某个泛型（用于检查类型是否实现了指定的原始泛型接口或继承自指定的原始泛型类。
        /// </summary>
        /// <param name="type">要检查的类型</param>
        /// <param name="generic">指定的原始泛型类型</param>
        /// <returns>bool</returns>
        public static bool HasImplementedRawGeneric(this Type type, Type generic)
        {
            // 检查接口类型
            var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
            /*  获取类型的所有接口，并使用 LINQ 的 Any 方法检查是否有接口是指定的原始泛型类型。
                如果是，则返回 true，表示类型实现了指定的原始泛型接口。*/
            if (isTheRawGenericType) return true;

            //如果没有在接口中找到指定的原始泛型类型，则继续检查类型本身。

            // 检查类型
            while (type != null && type != typeof(object))
            {
                /*  使用一个循环来沿着类型的继承链向上遍历，直到类型为 object 或为 null。
                    在每次迭代中，方法都会检查当前类型是否是指定的原始泛型类型，如果是，则返回 true。*/
                isTheRawGenericType = IsTheRawGenericType(type);
                if (isTheRawGenericType) return true;
                type = type.BaseType;
            }
            /*  如果在接口和类型的继承链中都没有找到指定的原始泛型类型，则返回 false，
                表示类型没有实现指定的原始泛型接口或继承自指定的原始泛型类。*/
            return false;

            // 判断逻辑（用于判断给定的类型是否是指定的原始泛型类型。
            bool IsTheRawGenericType(Type t) => generic == (t.IsGenericType ? t.GetGenericTypeDefinition() : t);
        }

        /// <summary>
        /// 用于获取泛型类型的名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetGenericTypeName(this Type type)
        //用于获取给定类型的泛型类型名称。它接受一个 Type 对象作为参数，并返回一个字符串，表示泛型类型的名称。
        {
            var typeName = string.Empty;

            if (type.IsGenericType)
            /* 如果给定的类型是泛型类型（IsGenericType 为 true），则通过
               调用 type.GetGenericArguments() 获取泛型参数的数组。
               然后，使用 LINQ 的 Select 方法将每个泛型参数的名称提取出来，并使用 ToArray 方法将结果转换为字符串数组。*/
            {
                var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
                //接下来，使用 type.Name 获取类型的名称，并通过调用 Remove 方法删除名称中泛型参数部分的字符（''）。
                typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
            }
            else
            {
                typeName = type.Name;
            }

            return typeName;
        }

        public static string GetGenericTypeName(this object @object)
        /* 用于获取给定对象的泛型类型名称。它接受一个对象作为参数，
           并使用 GetType() 方法获取对象的类型，并调用 GetGenericTypeName(Type type) 方法来获取泛型类型名称。*/
        {
            return @object.GetType().GetGenericTypeName();
        }
    }
}