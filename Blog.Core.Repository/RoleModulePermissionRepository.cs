using Blog.Core.IRepository;
using Blog.Core.Model.Models;
using Blog.Core.Repository.Base;
using SqlSugar;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blog.Core.Repository.UnitOfWorks;

namespace Blog.Core.Repository
{
    /// <summary>
    /// RoleModulePermissionRepository
    /// </summary>	
    public class RoleModulePermissionRepository : BaseRepository<RoleModulePermission>, IRoleModulePermissionRepository
    {
        public RoleModulePermissionRepository(IUnitOfWorkManage unitOfWorkManage) : base(unitOfWorkManage)
        {
        }

        /// <summary>
        /// 多表查询
        /// </summary>
        /// <returns></returns>
        public async Task<List<TestMuchTableResult>> QueryMuchTable()
        {
            return await QueryMuch<RoleModulePermission, Modules, Permission, TestMuchTableResult>(
                (rmp, m, p) => new object[] {
                    JoinType.Left, rmp.ModuleId == m.Id,
                    JoinType.Left,  rmp.PermissionId == p.Id
                },

                (rmp, m, p) => new TestMuchTableResult()
                {
                    moduleName = m.Name,
                    permName = p.Name,
                    rid = rmp.RoleId,
                    mid = rmp.ModuleId,
                    pid = rmp.PermissionId
                },

                (rmp, m, p) => rmp.IsDeleted == false
                );
        }

        /// <summary>
        /// 角色权限Map
        /// RoleModulePermission, Module, Role 三表联合
        /// 第四个类型 RoleModulePermission 是返回值
        /// </summary>
        /// <returns></returns>
        public async Task<List<RoleModulePermission>> RoleModuleMaps()
        {
            return await QueryMuch<RoleModulePermission, Modules, Role, RoleModulePermission>(
                (rmp, m, r) => new object[] {
                    JoinType.Left, rmp.ModuleId == m.Id,
                    JoinType.Left,  rmp.RoleId == r.Id
                },

                (rmp, m, r) => new RoleModulePermission()
                {
                    Role = r,
                    Module = m,
                    IsDeleted = rmp.IsDeleted
                },

                (rmp, m, r) => rmp.IsDeleted == false && m.IsDeleted == false && r.IsDeleted == false
                );
        }

        /// <summary>
        /// 查询出角色-菜单-接口关系表全部Map属性数据
        /// </summary>
        /// <returns></returns>
        public async Task<List<RoleModulePermission>> GetRMPMaps()
        {
            return await Db.Queryable<RoleModulePermission>() //查询RoleModulePermission表
                /*  这是一系列的映射操作，用于将查询结果中的外键属性映射为关联的对象。(去看RoleModulePermission类最下面的代码你就懂了
                    这些映射操作将ModuleId映射为Module对象，PermissionId映射为Permission对象，RoleId映射为Role对象。

                    假设有两个实体类：User和Role，它们之间存在一对多的关系，即一个用户可以拥有多个角色。
                    在数据库表中，User表包含一个外键字段RoleId，用于关联Role表。

                    现在，我们要查询用户列表，并将每个用户的角色信息一起查询出来。

                    public class User
                    {
                        public int Id { get; set; }
                        public string Name { get; set; }
                        public int RoleId { get; set; }
                        public Role Role { get; set; }
                    }

                    public class Role
                    {
                        public int Id { get; set; }
                        public string Name { get; set; }
                    }
                    使用ORM库进行查询操作时，可以使用.Mapper()方法将外键字段RoleId映射为Role对象，以实现关联查询。
                    var userList = await Db.Queryable<User>()
                    .Mapper(u => u.Role, u => u.RoleId)
                    .ToListAsync();
                    在上面的示例中，.Mapper()方法的第一个参数是要映射的属性，第二个参数是属性对应的外键字段。
                    通过.Mapper(u => u.Role, u => u.RoleId)将RoleId映射为Role对象，这样查询结果中的每个User对象就会包含关联的Role对象。*/
                .Mapper(rmp => rmp.Module, rmp => rmp.ModuleId)
                .Mapper(rmp => rmp.Permission, rmp => rmp.PermissionId)
                .Mapper(rmp => rmp.Role, rmp => rmp.RoleId)
                .Where(d => d.IsDeleted == false) //过滤掉IsDeleted属性为false的记录
                .ToListAsync();
        }

        /// <summary>
        /// 查询出角色-菜单-接口关系表全部Map属性数据（分页查询
        /// </summary>
        /// <returns></returns>
        public async Task<List<RoleModulePermission>> GetRMPMapsPage()
        {
            return await Db.Queryable<RoleModulePermission>()
                .Mapper(rmp => rmp.Module, rmp => rmp.ModuleId)
                .Mapper(rmp => rmp.Permission, rmp => rmp.PermissionId)
                .Mapper(rmp => rmp.Role, rmp => rmp.RoleId)
                .ToPageListAsync(1, 5, 10);
            /*  第一个参数 1 表示要查询的页码，即要获取的页数。
                第二个参数 5 表示每页的记录数，即每页显示的数据条数。
                第三个参数 10 表示要获取的总记录数。*/
        }

        /// <summary>
        /// 批量更新菜单与接口的关系
        /// </summary>
        /// <param name="permissionId">菜单主键</param>
        /// <param name="moduleId">接口主键</param>
        /// <returns></returns>
        public async Task UpdateModuleId(long permissionId, long moduleId)
        {
            //并指定更新的目标表为 RoleModulePermission，并且筛选出接口ID等于给定 moduleId 的记录。
            await Db.Updateable<RoleModulePermission>(it => it.ModuleId == moduleId).
                //使得只有权限ID等于给定 permissionId 的记录才会被更新。
                Where(it => it.PermissionId == permissionId).ExecuteCommandAsync();
        }
    }

}