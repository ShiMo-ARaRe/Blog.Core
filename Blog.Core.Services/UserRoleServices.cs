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
        /// �����û�ID�ͽ�ɫID����ѯUserRole�����򷵻أ��������
        /// </summary>
        /// <param name="uid">�û�ID</param>
        /// <param name="rid">��ɫID</param>
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
        /// �����û�ID��ȡ��ɫID
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        [Caching(AbsoluteExpiration = 30)] //����һ���������ԣ����ڽ������Ľ�����������������ʾ30���Ӻ���ڡ�
        public async Task<int> GetRoleIdByUid(long uid)
        {
            /*  OrderByDescending(d => d.Id)���Բ�ѯ�������Id���н�������

                LastOrDefault()����ȡ�����Ľ���е����һ��Ԫ�أ������ѯ���Ϊ�գ��򷵻�Ĭ��ֵ��

                ?.RoleId��������һ��Ԫ�ز�Ϊ�գ��򷵻���RoleId���Ե�ֵ��

                .ObjToInt()�������صĽ�ɫIDֵת��Ϊ�������͡�*/
            return ((await base.Query(d => d.UserId == uid)).OrderByDescending(d => d.Id).LastOrDefault()?.RoleId).ObjToInt();
        }
    }
}
