using Blog.Core.Common;
using Blog.Core.IRepository.Base;
using Blog.Core.IServices;
using Blog.Core.Model.Models;
using Blog.Core.Services.BASE;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Core.Services
{
    /// <summary>
    /// UserRoleServices
    /// </summary>	
    public class UserRoleServices : BaseServices<UserRole>, IUserRoleServices
    {
        /// <summary>
        /// 根据用户ID和角色ID来查询UserRole表，有则返回，无则添加
        /// </summary>
        /// <param name="uid">用户ID</param>
        /// <param name="rid">角色ID</param>
        /// <returns></returns>
        public async Task<UserRole> SaveUserRole(long uid, long rid)
        {
            UserRole userRole = new UserRole(uid, rid);

            UserRole model = new UserRole();
            var userList = await base.Query(a => a.UserId == userRole.UserId && a.RoleId == userRole.RoleId);
            if (userList.Count > 0)
            {
                model = userList.FirstOrDefault();
            }
            else
            {
                var id = await base.Add(userRole);
                model = await base.QueryById(id);
            }

            return model;

        }


        /// <summary>
        /// 根据用户ID获取角色ID
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        [Caching(AbsoluteExpiration = 30)] //这是一个缓存特性，用于将方法的结果缓存起来。这里表示30分钟后过期。
        public async Task<int> GetRoleIdByUid(long uid)
        {
            /*  OrderByDescending(d => d.Id)：对查询结果按照Id进行降序排序。

                LastOrDefault()：获取排序后的结果中的最后一个元素，如果查询结果为空，则返回默认值。

                ?.RoleId：如果最后一个元素不为空，则返回其RoleId属性的值。

                .ObjToInt()：将返回的角色ID值转换为整数类型。*/
            return ((await base.Query(d => d.UserId == uid)).OrderByDescending(d => d.Id).LastOrDefault()?.RoleId).ObjToInt();
        }
    }
}
