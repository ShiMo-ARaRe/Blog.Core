using Blog.Core.AuthHelper;
using Blog.Core.Common;
using Blog.Core.Common.AppConfig;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace Blog.Core.Extensions
{
    /// <summary>
    /// 系统 授权服务 配置
    /// </summary>
    public static class AuthorizationSetup
    {
        public static void AddAuthorizationSetup(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));//检查 services 参数是否为空，如果为空则抛出异常

            // 以下四种常见的授权方式。

            // 1、这个很简单，其他什么都不用做， 只需要在API层的controller上边，增加特性即可
            // [Authorize(Roles = "Admin,System")]


            // 2、这个和上边的异曲同工，好处就是不用在controller中，写多个 roles 。
            // 然后这么写 [Authorize(Policy = "Admin")]
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Client", policy => policy.RequireRole("Client").Build());
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin").Build());
                options.AddPolicy("SystemOrAdmin", policy => policy.RequireRole("Admin", "System"));
                options.AddPolicy("A_S_O", policy => policy.RequireRole("Admin", "System", "Others"));
            });




            #region 参数
            //读取配置文件
            var symmetricKeyAsBase64 = AppSecretConfig.Audience_Secret_String;  //拿取密钥
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);   //将密钥转换为字节数组
            var signingKey = new SymmetricSecurityKey(keyByteArray);    //准备加密key
            var Issuer = AppSettings.app(new string[] { "Audience", "Issuer" });    //发行人
            var Audience = AppSettings.app(new string[] { "Audience", "Audience" });    //听众

            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256); //签名凭据

            // 如果要数据库动态绑定，这里先留个空，后边处理器里动态赋值
            var permission = new List<PermissionItem>();    //权限项列表

            // 角色与接口的权限要求参数
            var permissionRequirement = new PermissionRequirement(
                "/api/denied",// 拒绝授权的跳转地址（目前无用）
                permission,
                ClaimTypes.Role,//基于角色的授权
                Issuer,//发行人
                Audience,//听众
                signingCredentials,//签名凭据
                expiration: TimeSpan.FromSeconds(60 * 60)//接口的过期时间
                );
            #endregion
            // 3、自定义复杂的策略授权
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Permissions.Name,
                         policy => policy.Requirements.Add(permissionRequirement));//策略授权扩展，把代码逻辑放到其他文件中
            });


            // 4、基于Scope策略授权
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Scope_BlogModule_Policy", builder =>
            //    {
            //        //客户端Scope中包含blog.core.api.BlogModule才能访问
            //        // 同时引用nuget包：IdentityServer4.AccessTokenValidation
            //        builder.RequireScope("blog.core.api.BlogModule");
            //    });

            //    // 其他 Scope 策略
            //    // ...

            //});

            // 这里冗余写了一次,因为很多人看不到
            #region http
            /*  为什么 将<IHttpContextAccessor, HttpContextAccessor> 注册成单例服务？
                提高性能：每个请求到达时，ASP.NET Core 会创建一个新的 HttpContext 对象，其中包含了当前请求的上下文信息。

                HttpContextAccessor 实例的作用是提供对当前请求的 HttpContext 对象的访问。
                当我们在应用程序的其他组件中获取 IHttpContextAccessor 实例，并通过它访问 HttpContext 属性时，实际上获取到的是当前请求的 HttpContext 对象。

                由于每个请求都有自己的 HttpContext 对象，因此不同请求之间的 HttpContext 是相互隔离的。
                这意味着每个请求可以独立地访问和管理自己的上下文信息，而不会相互干扰。

                如果 IHttpContextAccessor 是瞬态或作用域服务，那么每次请求都会创建一个新的 HttpContextAccessor 实例，这可能会导致性能下降。
                将 IHttpContextAccessor 注册为单例服务时，所有请求共享相同的 HttpContextAccessor 实例，但每个请求都会有自己独立的 HttpContext 对象。
             */
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            /*  当其他组件需要访问当前 HTTP 上下文时，它们可以通过依赖注入方式获取 IHttpContextAccessor 实例，并使用该实例访问当前 请求的上下文信息。
                上下文信息通过 HttpContext 对象暴露给应用程序，当前请求的上下文信息包括以下内容：
               
                Request（请求）：
                请求方法（GET、POST、PUT、DELETE 等）。
                请求的 URL、路径和查询字符串。
                请求的头部信息（User-Agent、Content-Type 等）。
                请求的内容（请求体）。

                Response（响应）：
                响应的状态码（200、404、500 等）。
                响应的头部信息（Content-Type、Cache-Control 等）。
                响应的内容（响应体）。

                User（用户）：
                用户身份验证信息（Claims）。
                用户角色和权限信息。
                用户标识信息（用户 ID、用户名等）。

                Session（会话）：
                会话状态信息。
                会话过期时间。
                会话数据（存储在服务器端的用户特定数据）。

                Cookies（Cookie）：
                请求中的 Cookie。
                响应中设置的 Cookie。

                Connection（连接）：
                客户端 IP 地址。
                请求的协议（HTTP、HTTPS）。
                客户端和服务器之间的连接信息。

                Server（服务器）：
                服务器信息（主机名、端口号等）。
                应用程序的环境变量和配置信息。
            */
            #endregion

            #region 注入权限处理器
            /*  PermissionHandler 是一个自定义的授权处理器，
             *  它继承自 AuthorizationHandler<>类，所以实现了 IAuthorizationHandler 接口，
             *  用于处理与权限要求相关的授权逻辑。*/
            services.AddScoped<IAuthorizationHandler, PermissionHandler>();
            /*  注册了一个单例服务，将一个新的 PermissionRequirement 对象添加到服务容器中。
             *  PermissionRequirement 是一个封装了 权限项 的类。
             *  通过将它注册为单例服务，可以在应用程序中的其他地方获取到同一个 PermissionRequirement 实例，
             *  确保在整个应用程序中 共享相同的 权限项列表。*/
            services.AddSingleton(permissionRequirement);
            #endregion
        }
    }
}
