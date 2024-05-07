namespace Blog.Core.Model.Models
{

    public class TestMuchTableResult
    {
        /// <summary>
        /// 存储接口名
        /// </summary>
        public string moduleName { get; set; }
        /// <summary>
        /// 存储路由菜单名
        /// </summary>
        public string permName { get; set; }
        /// <summary>
        /// 存储角色的ID
        /// </summary>
        public long rid { get; set; }
        /// <summary>
        /// 存储接口的ID
        /// </summary>
        public long mid { get; set; }
        /// <summary>
        /// 用于路由菜单的ID
        /// </summary>
        public long? pid { get; set; }
    }
}
