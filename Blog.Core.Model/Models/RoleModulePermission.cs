using SqlSugar;
using System;

/*  在许多系统中，RoleModulePermission表，Role表，Modules表和Permission表通常用于实现角色基础的权限管理。
    这四个表之间的关系，你去执行一下这个就懂了：

-- 菜单执行Action名 代表 路由操作，由前端处理。
-- 接口地址 代表 API请求：由后端处理。
SELECT
    r.Name AS 角色名,
    m.Name AS 接口名称,
    m.LinkUrl AS 接口地址,
    p.Name AS 路由菜单名,
    p.Code AS 菜单执行Action名,
    rmp.RoleId,
    rmp.ModuleId,
    rmp.PermissionId
FROM
    rolemodulepermission AS rmp
JOIN
    role AS r ON rmp.roleid = r.id
JOIN
    modules AS m ON rmp.ModuleId = m.Id
JOIN
    permission AS p ON rmp.PermissionId = p.Id;
 */

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 按钮跟权限关联表
    /// </summary>
    public class RoleModulePermission : RoleModulePermissionRoot<long>
    {
        public RoleModulePermission()
        {
            //this.Role = new Role();
            //this.Module = new Module();
            //this.Permission = new Permission();

        }

        /// <summary>
        ///获取或设置是否禁用，逻辑上的删除，非物理删除
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsDeleted { get; set; }

        /// <summary>
        /// 创建ID
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public long? CreateId { get; set; }
        /// <summary>
        /// 创建者
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string CreateBy { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? CreateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// 修改ID
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public long? ModifyId { get; set; }
        /// <summary>
        /// 修改者
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string ModifyBy { get; set; }
        /// <summary>
        /// 修改时间
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? ModifyTime { get; set; } = DateTime.Now;

        // 下边三个实体参数，只是做传参作用，所以忽略下
        [SugarColumn(IsIgnore = true)]
        public Role Role { get; set; }
        [SugarColumn(IsIgnore = true)]
        public Modules Module { get; set; }
        [SugarColumn(IsIgnore = true)]
        public Permission Permission { get; set; }
    }
}
