using Blog.Core.Common;
using Blog.Core.Common.Helper;
using Blog.Core.Common.HttpContextUser;
using Blog.Core.IServices;
using Blog.Core.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blog.Core.Common.Swagger;
using Blog.Core.Model.Models;

namespace Blog.Core.AuthHelper
{
    /// <summary>
    /// 权限授权处理器
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        /// <summary>
        /// 验证方案提供对象
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; set; }

        private readonly IRoleModulePermissionServices _roleModulePermissionServices;   //角色-接口-菜单服务实例
        private readonly IHttpContextAccessor _accessor;    //用于获取当前请求上下文
        private readonly ISysUserInfoServices _userServices;    //用户服务实例
        private readonly IUser _user;

        /// <summary>
        /// 构造函数注入
        /// </summary>
        /// <param name="schemes"></param>
        /// <param name="roleModulePermissionServices"></param>
        /// <param name="accessor"></param>
        /// <param name="userServices"></param>
        /// <param name="user"></param>
        public PermissionHandler(IAuthenticationSchemeProvider schemes,
            IRoleModulePermissionServices roleModulePermissionServices, IHttpContextAccessor accessor,
            ISysUserInfoServices userServices, IUser user)
        {
            _accessor = accessor;
            _userServices = userServices;
            _user = user;
            Schemes = schemes;
            _roleModulePermissionServices = roleModulePermissionServices;
        }

        // 重写异步处理程序
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            var httpContext = _accessor.HttpContext;    // 获取当前请求上下文信息

            #region 获取系统中 所有的角色和菜单 的关系集合（权限项列表）
            if (!requirement.Permissions.Any()) // 如果一个都没有，则重新获取
            {
                var data = await _roleModulePermissionServices.RoleModuleMaps();//获取 所有权限项信息（封装到了 List<RoleModulePermission>中
                var list = new List<PermissionItem>();// 打算转移到 权限项列表 中
                // ids4和jwt切换
                // ids4
                if (Permissions.IsUseIds4)
                {
                    list = (from item in data
                            where item.IsDeleted == false
                            orderby item.Id
                            select new PermissionItem
                            {
                                Url = item.Module?.LinkUrl,
                                Role = item.Role?.Id.ObjToString(),
                            }).ToList();
                }
                // jwt
                else
                {
                    list = (from item in data
                            where item.IsDeleted == false
                            orderby item.Id
                            select new PermissionItem
                            {
                                Url = item.Module?.LinkUrl,
                                Role = item.Role?.Name.ObjToString(),
                            }).ToList();// 这是转移过程
                }

                requirement.Permissions = list;
            }
            #endregion

            if (httpContext != null)//如果成功获取上下文信息
            {
                var questUrl = httpContext.Request.Path.Value.ToLower();// 获取当前请求 URL 的路径部分，也就是域名后面的部分。

                // 整体结构类似认证中间件UseAuthentication的逻辑，具体查看开源地址
                // https://github.com/dotnet/aspnetcore/blob/master/src/Security/Authentication/Core/src/AuthenticationMiddleware.cs
                httpContext.Features.Set<IAuthenticationFeature>(new AuthenticationFeature
                /* 这是 ASP.NET Core 中的一个方法，用于在 当前的 HttpContext 中设置特定的特性。
             在这个例子中，我们设置的是 IAuthenticationFeature。
             new AuthenticationFeature {...}：这是创建一个新的 AuthenticationFeature 实例，并设置其属性。
             AuthenticationFeature 是一个实现了 IAuthenticationFeature 接口的类，它包含了关于认证的信息。

             总的来说，这段代码的作用是在当前的 HttpContext 中设置一个新的 AuthenticationFeature，
             并将当前请求的路径和基路径保存在这个特性中。
             这样，如果在后续的处理过程中需要使用到原始的路径和基路径，就可以从这个特性中获取。
             这在处理认证和授权的逻辑中可能会非常有用。
             例如，如果用户在未经授权的情况下访问了一个受保护的资源，
             我们可能会将用户重定向到登录页面，并在用户登录成功后，
             再将用户重定向回他们原来试图访问的页面。
             在这种情况下，我们就需要知道用户原来试图访问的页面的路径，
             而这个路径就保存在 AuthenticationFeature 的 OriginalPath 属性中。
             同样，OriginalPathBase 属性也可以用于类似的目的。*/
                {
                    //这行代码将当前请求的 路径 设置为 AuthenticationFeature 的 OriginalPath 属性。
                    OriginalPath = httpContext.Request.Path,
                    //这行代码将当前请求的 基路径 设置为 AuthenticationFeature 的 OriginalPathBase 属性。
                    OriginalPathBase = httpContext.Request.PathBase
                });

                // Give any IAuthenticationRequestHandler schemes a chance to handle the request
                #region 远程验证
                // 主要作用是: 判断当前是否需要进行远程验证，如果是就进行远程验证
                //获取一个 IAuthenticationHandlerProvider 实例
                var handlers = httpContext.RequestServices.GetRequiredService<IAuthenticationHandlerProvider>();
                //使用 Schemes.GetRequestHandlerSchemesAsync() 方法获取所有在请求中注册的身份验证方案。
                foreach (var scheme in await Schemes.GetRequestHandlerSchemesAsync())
                //遍历每个身份验证方案，并尝试获取相应的处理程序：
                {
                    /*  使用 handlers.GetHandlerAsync(httpContext, scheme.Name) 方法获取与当前身份验证方案关联的处理程序。
                        如果获取到的处理程序是实现了 IAuthenticationRequestHandler 接口的对象，
                        并且成功执行了 HandleRequestAsync() 方法（返回 true），则表示该处理程序处理了当前请求。*/
                    if (await handlers.GetHandlerAsync(httpContext, scheme.Name) is IAuthenticationRequestHandler
                            handler && await handler.HandleRequestAsync())
                    {
                        context.Fail();//表示身份验证失败
                        //这将导致后续的身份验证中间件不会执行，并且控制流将返回到调用身份验证的代码。
                        return;
                    }
                }
                #endregion

                //判断请求是否拥有凭据，即有没有登录
                /*  .GetDefaultAuthenticateSchemeAsync() 方法 会返回 默认的 身份验证方案。
                  默认身份验证方案 是在 应用程序启动时 通过 身份验证中间件（Authentication Middleware）进行配置的。
                  因为我们在 AddAuthentication 方法中设置的 参数 是 JwtBearerDefaults.AuthenticationScheme，
                  所以获取到的将是配置的 JWT Bearer 身份验证方案。
                  该方法返回一个 Task<AuthenticationScheme>，表示获取 默认身份验证方案 的异步操作。

                  AuthenticationScheme 类是一个包含身份验证方案信息的类，它包括以下属性：

                  Name：一个字符串，表示身份验证方案的名称。

                  DisplayName：一个字符串，表示身份验证方案的显示名称。

                  HandlerType：一个 Type 对象，表示实现该身份验证方案的处理程序类型。*/
                var defaultAuthenticate = await Schemes.GetDefaultAuthenticateSchemeAsync();
                if (defaultAuthenticate != null)
                {
                    /*  该方法会对当前的请求进行身份验证，其所需的参数就是 身份验证方案的名称 （Bearer）
                       返回值为 AuthenticateResult 类型，它是身份验证结果的表示，包含了身份验证操作的结果信息。
                       AuthenticateResult 类的主要属性和方法：

                       Succeeded：一个布尔值，指示身份验证是否成功。如果身份验证成功，则为 true，否则为 false。
                       Principal：ClaimsPrincipal 对象，表示已验证的用户主体。它包含了用户的身份信息，如用户名、角色等。
                       Failure：一个 Exception 对象，表示身份验证过程中发生的错误。如果身份验证成功，该属性为 null。
                       None：一个静态只读的 AuthenticateResult 对象，表示未进行身份验证。
                       Success：一个静态只读的 AuthenticateResult 对象，表示身份验证成功。
                       NoResult：一个静态只读的 AuthenticateResult 对象，表示无法获取身份验证结果。 */
                    var result = await httpContext.AuthenticateAsync(defaultAuthenticate.Name);

                    // 是否开启测试环境
                    var isTestCurrent = AppSettings.app(new string[] { "AppSettings", "UseLoadTest" }).ObjToBool();//从配置中获取...

                    /*   result?.Principal不为空即登录成功，如果是 测试模式 我们也让它通过
                         Principal：ClaimsPrincipal 对象，表示已验证的用户主体。它包含了用户的身份信息，如用户名、角色等。*/
                    if (result?.Principal != null || isTestCurrent || httpContext.IsSuccessSwagger())
                    {
                        if (!isTestCurrent) httpContext.User = result.Principal;

                        //应该要先校验用户的信息 再校验菜单权限相关的
                        // JWT模式下校验当前用户状态
                        // IDS4也可以校验，可以通过服务或者接口形式
                        SysUserInfo user = new();
                        if (!Permissions.IsUseIds4)
                        {
                            //校验用户
                            user = await _userServices.QueryById(_user.ID, true); //通过用户的 ID 查询用户信息
                            if (user == null)//用户是否存在
                            {
                                _user.MessageModel = new ApiResponse(StatusCode.CODE401, "用户不存在或已被删除").MessageModel;
                                context.Fail(new AuthorizationFailureReason(this, _user.MessageModel.msg));//表示授权失败，并传递授权失败原因。
                                return;
                            }

                            if (user.IsDeleted)//用户是否被删除
                            {
                                _user.MessageModel = new ApiResponse(StatusCode.CODE401, "用户已被删除,禁止登陆!").MessageModel;
                                context.Fail(new AuthorizationFailureReason(this, _user.MessageModel.msg));//表示授权失败，并传递授权失败原因。
                                return;
                            }

                            if (!user.Enable)//用户是否被激活
                            {
                                _user.MessageModel = new ApiResponse(StatusCode.CODE401, "用户已被禁用!禁止登陆!").MessageModel;
                                context.Fail(new AuthorizationFailureReason(this, _user.MessageModel.msg));//表示授权失败，并传递授权失败原因。
                                return;
                            }
                        }

                        // 判断token是否过期，过期则重新登录
                        var isExp = false;
                        // ids4和jwt切换
                        // ids4
                        if (Permissions.IsUseIds4)
                        {
                            isExp = (httpContext.User.Claims.FirstOrDefault(s => s.Type == "exp")?.Value) != null &&
                                    DateHelper.StampToDateTime(httpContext.User.Claims
                                        .FirstOrDefault(s => s.Type == "exp")?.Value) >= DateTime.Now;
                        }
                        else
                        {
                            // jwt
                            /*  访问 httpContext.User.Claims 属性来获取当前用户的声明集合
                                ClaimTypes.Expiration 是一个标准的声明类型，用于表示 JWT 的过期时间*/
                            isExp =
                                (httpContext.User.Claims.FirstOrDefault(s => s.Type == ClaimTypes.Expiration)
                                    ?.Value) != null && //判断声明是否存在
                                DateTime.Parse(httpContext.User.Claims
                                    .FirstOrDefault(s => s.Type == ClaimTypes.Expiration)?.Value) >= DateTime.Now;//再判断是否过期
                        }

                        if (!isExp)
                        {
                            /*  context.Fail() 方法用于将授权操作标记为失败，并提供一个失败原因。
                              context.Fail() 方法接受一个 AuthorizationFailureReason 对象作为参数，该对象描述了授权失败的原因。
                              this 参数表示当前的授权处理程序（authorization handler）。它指示哪个授权处理程序导致了授权失败。
                              "授权已过期,请重新授权" 字符串是授权失败的具体消息，它描述了授权过期的情况。*/
                            context.Fail(new AuthorizationFailureReason(this, "授权已过期,请重新授权"));
                            return;
                        }


                        #region 校验签发时间
                        if (!Permissions.IsUseIds4)
                        {
                            var value = httpContext.User.Claims //获取当前用户的所有声明
                                /*  使用 FirstOrDefault 方法查找具有类型
                                    为 JwtRegisteredClaimNames.Iat（JWT 注册的声明名称，表示签发时间）的第一个声明。*/
                                .FirstOrDefault(s => s.Type == JwtRegisteredClaimNames.Iat)?.Value;
                            if (value != null) //如果找到了该声明，并且其值不为 null
                            {
                                //  使用 value.ObjToDate() 方法，将声明的值转换为日期类型。
                                if (user.CriticalModifyTime > value.ObjToDate())//检查用户的 CriticalModifyTime 是否大于签发时间的日期值
                                //  如果用户的 CriticalModifyTime 大于签发时间，表示授权已失效
                                {
                                    _user.MessageModel = new ApiResponse(StatusCode.CODE401, "很抱歉,授权已失效,请重新授权")
                                        .MessageModel;//创建一个包含错误信息的 ApiResponse 对象，并将其赋值给 _user.MessageModel。
                                    //调用 context.Fail(new AuthorizationFailureReason(this, _user.MessageModel.msg))，表示授权失败，并传递授权失败原因。
                                    context.Fail(new AuthorizationFailureReason(this, _user.MessageModel.msg));
                                    return;//返回，终止后续的授权处理。
                                }
                            }
                        }
                        #endregion

                        // 获取当前用户的角色信息
                        var currentUserRoles = new List<string>();
                        currentUserRoles = (from item in httpContext.User.Claims //遍历每个声明
                                            where item.Type == ClaimTypes.Role  //筛选出类型为 ClaimTypes.Role 的声明
                                            select item.Value).ToList(); //获取符合条件的声明的值
                        if (!currentUserRoles.Any())//如果当前用户没有扮演任何角色，那就换个方式再搜索一遍
                        {
                            currentUserRoles = (from item in httpContext.User.Claims
                                                where item.Type == "role"   //这是为了处理非标准的角色声明类型
                                                select item.Value).ToList();
                        }

                        //超级管理员 默认拥有所有权限（如果用户拥有的角色 包括 超级管理员的话，那就不会执行下面{}中的代码。
                        if (currentUserRoles.All(s => s != "SuperAdmin"))
                        {
                            var isMatchRole = false;    //标识用户是否有资格使用该API请求方法
                            /* requirement.Permissions 是一个权限列表（权限项列表），
                               每个权限对象包含了角色和与之关联的 URL。
                               根据用户所扮演的 所有角色 获取 其关联 的 所有权限项列表。   */
                            var permisssionRoles =
                                requirement.Permissions.Where(w => currentUserRoles.Contains(w.Role));
                            foreach (var item in permisssionRoles)  //遍历筛选后的权限项列表
                            {
                                try
                                {
                                    /*  Regex.Match() 方法尝试使用正则表达式进行匹配。
                                        item.Url?.ObjToString().ToLower())?.Value 用于提取出每个权限项的Url。
                                        若某个 权限项的Url 等于 当前请求的Url，
                                        那么就说明该用户有资格发起该请求，立即中断遍历，后续执行API方法  */
                                    if (Regex.Match(questUrl, item.Url?.ObjToString().ToLower())?.Value == questUrl)
                                    {
                                        isMatchRole = true;
                                        break;
                                    }
                                }
                                catch (Exception)
                                {
                                    // ignored
                                }
                            }

                            //验证权限
                            if (currentUserRoles.Count <= 0 || !isMatchRole)
                            //若用户没有扮演任何角色 或者 isMatchRole为假的话，说明发起该请求的用户没有资格使用该API方法
                            {
                                context.Fail(); //失败
                                return; //并结束
                            }
                        }

                        context.Succeed(requirement);   //将授权操作标记为成功
                        return;
                    }
                }
                /*  检查当前 HTTP 请求的方法是否为 "POST"。
                  Equals("POST") 是一个字符串比较操作，用于判断请求的方法是否为 "POST"。
                  检查当前 HTTP 请求是否具有表单数据的内容类型。
                  HasFormContentType 是一个布尔值属性，如果请求的内容类型是表单数据类型，则返回 true，否则返回 false。*/
                //判断没有登录时，是否访问登录的url,并且是Post请求，并且是form表单提交类型，否则为失败
                if (!(questUrl.Equals(requirement.LoginPath.ToLower(), StringComparison.Ordinal) &&
                      (!httpContext.Request.Method.Equals("POST") || !httpContext.Request.HasFormContentType)))
                {
                    context.Fail();//context.Fail() 方法会将授权处理上下文标记为失败，表示请求未通过授权验证。然后，方法会立即返回
                    return;
                }
            }

            //context.Succeed(requirement);
        }
    }
}