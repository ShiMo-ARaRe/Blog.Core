using Blog.Core.Common.HttpContextUser;
using Blog.Core.IServices;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Blog.Core.Extensions.Authorizations.Behaviors
{
    /// <summary>
    /// 用于处理与用户行为相关的操作和逻辑
    /// </summary>
    public class UserBehaviorService : IUserBehaviorService
    {
        private readonly IUser _user;
        private readonly ISysUserInfoServices _sysUserInfoServices;
        private readonly ILogger<UserBehaviorService> _logger;
        private readonly string _uid;
        private readonly string _token;

        public UserBehaviorService(IUser user
            , ISysUserInfoServices sysUserInfoServices
            , ILogger<UserBehaviorService> logger)
        {
            _user = user;
            _sysUserInfoServices = sysUserInfoServices;
            _logger = logger;
            _uid = _user.ID.ObjToString();
            _token = _user.GetToken();
        }

        //下面这些方法都是占位方法，未实现具体的功能。它们抛出了一个 System.NotImplementedException 异常，表示这些方法还没有被实现。

        /// <summary>
        /// 检查令牌是否正常
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<bool> CheckTokenIsNormal()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 检查用户是否正常
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<bool> CheckUserIsNormal()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 创建或更新用户访问权限
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<bool> CreateOrUpdateUserAccessByUid()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 移除用户的所有访问权限
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<bool> RemoveAllUserAccessByUid()
        {
            throw new System.NotImplementedException();
        }
    }
}
