using Blog.Core.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Blog.Core.Common.HttpContextUser;

namespace Blog.Core.AuthHelper
{
    /// <summary>
    /// 在 身份验证失败时 或者 用户没有足够权限时 就会走这里
    /// 这是一个自定义身份验证处理器类。它用于处理身份验证过程中的不同情况，并生成相应的 API 响应。
    /// </summary>
    public class ApiResponseHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUser _user;   //用于获取当前用户信息

        public ApiResponseHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IUser user) : base(options, logger, encoder, clock)
        {
            _user = user;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        //该方法未被实现
        {
            throw new NotImplementedException();
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        //当身份验证失败或未进行身份验证时，会调用此方法
        {
            Response.ContentType = "application/json";
            //设置响应的内容类型为 JSON，并将状态码设置为 401 未授权。
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            //使用 ApiResponse 类创建一个包含 CODE401 状态码的响应，并将其序列化为 JSON 字符串，然后写入响应中。
            await Response.WriteAsync(JsonConvert.SerializeObject((new ApiResponse(StatusCode.CODE401)).MessageModel));
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        //当用户没有足够的权限（禁止访问）时，会调用此方法来处理禁止访问情况。
        {
            Response.ContentType = "application/json";  //设置响应的内容类型为 JSON。
            if (_user.MessageModel != null)
            {
                /*  检查 _user.MessageModel 是否为非空。
                    如果存在，表示用户具有自定义的消息模型，将响应状态码设置为该模型中的状态码，并将模型序列化为 JSON 字符串写入响应中。*/
                Response.StatusCode = _user.MessageModel.status;
                await Response.WriteAsync(JsonConvert.SerializeObject(_user.MessageModel));
            }
            else
            {
                /*  如果 _user.MessageModel 为空，表示用户没有自定义的消息模型，将响应状态码设置为 403 禁止访问，
                    并将 CODE403 状态码的响应序列化为 JSON 字符串写入响应中。*/
                Response.StatusCode = StatusCodes.Status403Forbidden;
                await Response.WriteAsync(JsonConvert.SerializeObject((new ApiResponse(StatusCode.CODE403)).MessageModel));
            }
        }
    }
}