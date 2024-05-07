using Blog.Core.AuthHelper;
using Blog.Core.Common;
using Blog.Core.Common.AppConfig;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Blog.Core.Extensions
{
    /// <summary>
    /// JWT权限 认证服务（鉴权服务
    /// </summary>
    public static class Authentication_JWTSetup
    {
        public static void AddAuthentication_JWTSetup(this IServiceCollection services)
        {
            //检查 services 参数是否为空，如果为空则抛出异常。
            if (services == null) throw new ArgumentNullException(nameof(services));

            //读取配置文件
            var symmetricKeyAsBase64 = AppSecretConfig.Audience_Secret_String;  //拿取密钥
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);   //将密钥转换为字节数组
            var signingKey = new SymmetricSecurityKey(keyByteArray);    //准备加密key
            var Issuer = AppSettings.app(new string[] { "Audience", "Issuer" });    //发行人
            var Audience = AppSettings.app(new string[] { "Audience", "Audience" });    //听众

            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256); //签名凭据

            // 令牌验证参数
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,//表示验证发行人的签名密钥
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,//表示验证发行人（Issuer）
                ValidIssuer = Issuer,//发行人
                ValidateAudience = true,//表示验证订阅人（Audience）
                ValidAudience = Audience,//订阅人
                ValidateLifetime = true,//表示验证令牌的有效期
                ClockSkew = TimeSpan.FromSeconds(30),//表示允许的时钟偏差时间
                RequireExpirationTime = true,//表示要求令牌包含过期时间
            };

            // 开启Bearer认证
            services.AddAuthentication(o =>
            {
                /*  将默认的身份验证方案设置为 JwtBearerDefaults.AuthenticationScheme，即 JWT Bearer 身份验证方案。
                    这意味着当没有显式指定身份验证方案时，将使用 JWT Bearer 方案进行身份验证。*/
                o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                /*  将默认的挑战方案设置为 nameof(ApiResponseHandler)。
                    指定在身份验证失败时使用的默认挑战方案。
                    nameof(ApiResponseHandler) 是一个字符串，表示自定义的身份验证处理器类 ApiResponseHandler 的名称。*/
                o.DefaultChallengeScheme = nameof(ApiResponseHandler);
                /*  将默认的禁止访问方案设置为 nameof(ApiResponseHandler)。
                    指定在用户没有足够权限时使用的默认禁止访问方案。
                    nameof(ApiResponseHandler) 是一个字符串，表示自定义的身份验证处理器类 ApiResponseHandler 的名称。*/
                o.DefaultForbidScheme = nameof(ApiResponseHandler);
            })
             // 添加JwtBearer服务
             .AddJwtBearer(o =>
             {
                 /* TokenValidationParameters 属性：
                    将前面创建的 tokenValidationParameters 对象赋值给该属性。
                    该对象包含了令牌验证的参数，用于验证接收到的令牌的有效性和安全性。*/
                 o.TokenValidationParameters = tokenValidationParameters;
                 /* Events 属性：
                    为 JwtBearer 配置事件处理程序，处理不同的事件。
                    JwtBearerEvents 是一个包含各种身份验证事件的类。*/
                 o.Events = new JwtBearerEvents
                 {
                     /* OnMessageReceived 事件处理程序：
                        当接收到消息时触发，用于从 请求的查询字符串中 获取访问令牌  */
                     OnMessageReceived = context =>
                     {
                         //获取请求的查询字符串中名为 "access_token" 的参数值，并将其赋给 accessToken 变量。
                         var accessToken = context.Request.Query["access_token"];

                         // If the request is for our hub...
                         var path = context.HttpContext.Request.Path;   //获取请求的路径
                         /* 检查路径是否以 "/api2/chathub" 开头。
                            并且 accessToken 不为空或非空字符串，则表示该请求是发送到特定 Hub 的请求。*/
                         if (!string.IsNullOrEmpty(accessToken) &&
                             (path.StartsWithSegments("/api2/chathub")))
                         {
                             // Read the token out of the query string
                             context.Token = accessToken;//将查询字符串中的令牌设置为上下文的令牌。
                         }
                         return Task.CompletedTask;//表示事件处理程序已完成处理
                     },
                     /* OnChallenge 事件处理程序：
                        当身份验证失败时触发，用于设置响应头中的错误描述。
                        该处理程序将错误描述存储在响应头的 "Token-Error" 字段中。*/
                     OnChallenge = context =>
                     {
                         context.Response.Headers["Token-Error"] = context.ErrorDescription;
                         return Task.CompletedTask;//表示事件处理程序已完成处理
                     },
                     /* OnAuthenticationFailed 事件处理程序：
                        当身份验证失败时触发，用于处理身份验证失败的情况。
                        首先，它使用 JwtSecurityTokenHandler 对象解析从请求头中获取的令牌。
                        然后，它检查解析后的令牌的发行人（Issuer）和订阅人（Audience）是否与预期的值匹配，
                        如果不匹配，则在响应头中设置相应的错误信息。
                        最后，如果令牌过期，将在响应头中添加 "Token-Expired" 字段，表示令牌已过期。*/
                     OnAuthenticationFailed = context =>
                     {
                         //创建一个 JwtSecurityTokenHandler 实例，用于处理 JWT 令牌
                         var jwtHandler = new JwtSecurityTokenHandler();
                         /* 从请求头中获取令牌：
                            通过 context.Request.Headers["Authorization"] 获取请求头中的 "Authorization" 值，
                            该值通常包含了以 "Bearer " 开头的 JWT 令牌。
                            使用 ObjToString() 方法将请求头转换为字符串，
                            并通过 Replace("Bearer ", "") 去掉 "Bearer " 前缀，得到纯粹的令牌值。*/
                         var token = context.Request.Headers["Authorization"].ObjToString().Replace("Bearer ", "");
                         /* 检查令牌的有效性：
                            首先，使用 jwtHandler.CanReadToken(token) 方法验证令牌是否可以被成功解析读取。
                            如果令牌不为空且解析成功，则继续处理*/
                         if (token.IsNotEmptyOrNull() && jwtHandler.CanReadToken(token))
                         {
                             //将令牌解析为 JwtSecurityToken 对象。
                             var jwtToken = jwtHandler.ReadJwtToken(token);
                             /* 检查解析后的令牌的发行人是否与预期的发行人（Issuer）匹配，如果不匹配，
                                则在响应头中添加 "Token-Error-Iss" 字段，并设置错误消息为 "issuer is wrong!"。
                                检查解析后的令牌的订阅人是否与预期的订阅人（Audience）匹配，
                                如果不匹配，则在响应头中添加 "Token-Error-Aud" 字段，并设置错误消息为 "Audience is wrong!"。*/
                             if (jwtToken.Issuer != Issuer)
                             {
                                 context.Response.Headers["Token-Error-Iss"] = "issuer is wrong!";
                             }
                             if (jwtToken.Audiences.FirstOrDefault() != Audience)
                             {
                                 context.Response.Headers["Token-Error-Aud"] = "Audience is wrong!";
                             }
                         }


                         // 如果过期，则把<是否过期>添加到，返回头信息中
                         if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                         /* 检查令牌是否过期：
                            通过检查 context.Exception.GetType() 是否等于 typeof(SecurityTokenExpiredException) 来确定令牌是否过期。
                            如果令牌过期，将在响应头中添加 "Token-Expired" 字段，并设置值为 "true"，表示令牌已过期。*/
                         {
                             context.Response.Headers["Token-Expired"] = "true";
                         }
                         return Task.CompletedTask;//表示事件处理程序已完成处理
                     }
                 };
             })
             /* 使用 .AddScheme 方法将自定义的 ApiResponseHandler 添加为身份验证方案。
                使用 nameof(ApiResponseHandler) 获取自定义的身份验证处理器类 ApiResponseHandler 的名称。
                通过使用 AuthenticationSchemeOptions 类型和一个空的选项配置委托（o => { }）来注册方案。*/
             .AddScheme<AuthenticationSchemeOptions, ApiResponseHandler>(nameof(ApiResponseHandler), o => { });

        }
    }
}
