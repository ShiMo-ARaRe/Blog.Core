using System;
using System.Linq;

namespace Blog.Core.Common.Extensions
{
    /// <summary>
    /// 扩展了与泛型类型相关的方法
    /// </summary>
    public static class GenericTypeExtensions
    {
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

        /// <summary>
        /// 判断类型是否实现某个泛型
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="generic">泛型类型</param>
        /// <returns>bool</returns>
        // public static bool HasImplementedRawGeneric(this Type type, Type generic)
        // {
        //     // 检查接口类型
        //     var isTheRawGenericType = type.GetInterfaces().Any(IsTheRawGenericType);
        //     if (isTheRawGenericType) return true;

        //     // 检查类型
        //     while (type != null && type != typeof(object))
        //     {
        //         isTheRawGenericType = IsTheRawGenericType(type);
        //         if (isTheRawGenericType) return true;
        //         type = type.BaseType;
        //     }

        //     return false;

        //     // 判断逻辑
        //     bool IsTheRawGenericType(Type type) => generic == (type.IsGenericType ? type.GetGenericTypeDefinition() : type);
        // }
    }
}