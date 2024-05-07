
namespace Blog.Core.AuthHelper
{
    /// <summary>
    /// 用户或角色或其他凭据实体,就像是订单详情一样
    /// 之前的名字是 Permission
    /// </summary>
    public class PermissionItem //描述 权限项 的实体类
    {
        /*  通过创建 PermissionItem 类的实例，并设置相应的属性值，可以定义一组具体的权限项。
            这些 权限项 可以用于构建授权要求（PermissionRequirement），并在应用程序中
            实现对特定用户、角色或 URL 的访问控制。*/

        /// <summary>
        /// 用户或角色或其他凭据名称
        /// </summary>
        public virtual string Role { get; set; } // 是谁？
        /// <summary>
        /// 请求Url
        /// </summary>
        public virtual string Url { get; set; } // 这个 "谁" 能干什么？
    }
}
