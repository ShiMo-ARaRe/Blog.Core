using SqlSugar;
using System;
using System.Collections.Generic;

namespace Blog.Core.Model.Models
{
    /// <summary>
    /// 路由菜单表
    /// </summary>
    public class Permission : PermissionRoot<long>
    {
        public Permission()
        {
            //this.ModulePermission = new List<ModulePermission>();
            //this.RoleModulePermission = new List<RoleModulePermission>();
        }

        /// <summary>
        /// 菜单执行Action名（路由操作
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string Code { get; set; }
        /// <summary>
        /// 菜单显示名（如用户页、编辑(按钮)、删除(按钮)）
        /// </summary>
        [SugarColumn(Length = 50, IsNullable = true)]
        public string Name { get; set; }
        /// <summary>
        /// 是否是按钮
        /// </summary>
        public bool IsButton { get; set; } = false;
        /// <summary>
        /// 是否是隐藏菜单
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsHide { get; set; } = false;
        /// <summary>
        /// 是否keepAlive（是否存活
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IskeepAlive { get; set; } = false;


        /// <summary>
        /// 按钮事件
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string Func { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public int OrderSort { get; set; }
        /// <summary>
        /// 菜单图标
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string Icon { get; set; }
        /// <summary>
        /// 菜单描述    
        /// </summary>
        [SugarColumn(Length = 100, IsNullable = true)]
        public string Description { get; set; }
        /// <summary>
        /// 激活状态
        /// </summary>
        public bool Enabled { get; set; }
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

        /// <summary>
        ///获取或设置是否禁用，逻辑上的删除，非物理删除
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? IsDeleted { get; set; }

        /*  下面这些属性都被标记为[SugarColumn(IsIgnore = true)]，这意味着在使用Sugar ORM（一个对象关系映射库）与数据库进行交互时，
            这些属性将被忽略，它们不会映射到数据库表的任何列。*/
        [SugarColumn(IsIgnore = true)]
        public List<string> PnameArr { get; set; } = new List<string>();//存储路由菜单名的列表
        [SugarColumn(IsIgnore = true)]
        public List<string> PCodeArr { get; set; } = new List<string>();//存储菜单执行Action名的列表
        [SugarColumn(IsIgnore = true)]
        public string MName { get; set; }//存储接口名

        [SugarColumn(IsIgnore = true)]
        public bool hasChildren { get; set; } = true;
        //用于标记这个权限是否有子权限。这在构建权限树或者在用户界面中展示权限层级结构时可能有用。

        [SugarColumn(IsIgnore = true)]
        public List<Permission> Children { get; set; } = new List<Permission>();
        //用于存储这个权限的所有子权限。这在处理权限相关的逻辑时有用，例如，遍历一个权限及其所有子权限。

        [SugarColumn(IsIgnore = true)]
        public Modules Module { get; set; }//存储接口API地址信息

        //public virtual ICollection<ModulePermission> ModulePermission { get; set; }
        //public virtual ICollection<RoleModulePermission> RoleModulePermission { get; set; }
    }
}
