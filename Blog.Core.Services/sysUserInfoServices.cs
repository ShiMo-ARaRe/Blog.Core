using Blog.Core.IRepository.Base;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.Services.BASE;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Core.FrameWork.Services
{
    /// <summary>
    /// sysUserInfoServices
    /// </summary>	
    public class SysUserInfoServices : BaseServices<SysUserInfo>, ISysUserInfoServices
    {
        private readonly IBaseRepository<UserRole> _userRoleRepository;
        private readonly IBaseRepository<Role> _roleRepository;
        public SysUserInfoServices(IBaseRepository<UserRole> userRoleRepository, IBaseRepository<Role> roleRepository)
        {
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }
        /// <summary>
        /// 根据用户名和密码查询用户表，有则返回，无则添加
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="loginPwd"></param>
        /// <returns></returns>
        public async Task<SysUserInfo> SaveUserInfo(string loginName, string loginPwd)
        {
            SysUserInfo sysUserInfo = new SysUserInfo(loginName, loginPwd);
            SysUserInfo model = new SysUserInfo();
            var userList = await base.Query(a => a.LoginName == sysUserInfo.LoginName && a.LoginPWD == sysUserInfo.LoginPWD);
            if (userList.Count > 0)
            {
                model = userList.FirstOrDefault();
            }
            else
            {
                var id = await base.Add(sysUserInfo);
                model = await base.QueryById(id);
            }

            return model;

        }

        /// <summary>
        /// 根据用户名和密码获取用户的角色名
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="loginPwd"></param>
        /// <returns></returns>
        public async Task<string> GetUserRoleNameStr(string loginName, string loginPwd)
        {
            string roleName = ""; //存储角色名
            //通过FirstOrDefault方法获取第一个匹配的用户对象
            var user = (await base.Query(a => a.LoginName == loginName && a.LoginPWD == loginPwd)).FirstOrDefault();
            var roleList = await _roleRepository.Query(a => a.IsDeleted == false); //获取所有未删除的角色列表
            if (user != null)
            {
                var userRoles = await _userRoleRepository.Query(ur => ur.UserId == user.Id); //获取与该用户关联的用户角色列表
                if (userRoles.Count > 0)
                {
                    //构建一个角色ID的列表arr，通过userRoles集合中的每个元素的RoleId属性获取角色ID，并转换为字符串形式。
                    var arr = userRoles.Select(ur => ur.RoleId.ObjToString()).ToList();
                    //根据角色ID列表arr从角色列表roleList中筛选出对应的角色对象集合roles。
                    var roles = roleList.Where(d => arr.Contains(d.Id.ObjToString()));
                    //使用string.Join方法将roles集合中的每个角色对象的Name属性取出来，并用逗号连接成一个字符串，赋值给roleName。
                    roleName = string.Join(',', roles.Select(r => r.Name).ToArray());
                }
            }
            return roleName;
        }
    }
}

//----------sysUserInfo结束----------
