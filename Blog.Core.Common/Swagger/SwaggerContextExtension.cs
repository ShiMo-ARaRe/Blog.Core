using Blog.Core.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace Blog.Core.Common.Swagger;

/// <summary>
/// 提供了一些扩展方法，用于在Swagger上下文中处理会话和重定向等功能。
/// </summary>
public static class SwaggerContextExtension
{
	public const string SwaggerCodeKey = "swagger-code";
	public const string SwaggerJwt = "swagger-jwt";

    /// <summary>
    /// 检查当前应用程序的HttpContext中是否存在名为"swagger-code"的会话，并且其值为"success"。
    /// </summary>
    /// <returns></returns>
    public static bool IsSuccessSwagger()
	{
		return App.HttpContext?.GetSession()?.GetString(SwaggerCodeKey) == "success";
	}

    /// <summary>
    /// 检查给定的HttpContext对象中是否存在名为"swagger-code"的会话，并且其值为"success"。
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static bool IsSuccessSwagger(this HttpContext context)
	{
		return context.GetSession()?.GetString(SwaggerCodeKey) == "success";
	}

    /// <summary>
    /// 将当前应用程序的HttpContext中名为"swagger-code"的会话设置为"success"。
    /// </summary>
    public static void SuccessSwagger()
	{
		App.HttpContext?.GetSession()?.SetString(SwaggerCodeKey, "success");
	}

    /// <summary>
    /// 将给定的HttpContext对象中名为"swagger-code"的会话设置为"success"。
    /// </summary>
    /// <param name="context"></param>
    public static void SuccessSwagger(this HttpContext context)
	{
		context.GetSession()?.SetString(SwaggerCodeKey, "success");
	}

    /// <summary>
    /// 将给定的token字符串设置为给定的HttpContext对象中名为"swagger-jwt"的会话。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="token"></param>
    public static void SuccessSwaggerJwt(this HttpContext context, string token)
	{
		context.GetSession()?.SetString(SwaggerJwt, token);
	}

    /// <summary>
    /// 获取给定的HttpContext对象中名为"swagger-jwt"的会话的值。
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
	public static string GetSuccessSwaggerJwt(this HttpContext context)
	{
		return context.GetSession()?.GetString(SwaggerJwt);
	}

    /// <summary>
    /// 通过重定向，将当前请求的URL作为参数传递给"/swg-login.html"页面，用于Swagger登录。
    /// </summary>
    /// <param name="context"></param>
	public static void RedirectSwaggerLogin(this HttpContext context)
	{
		var returnUrl = context.Request.GetDisplayUrl(); //获取当前url地址 
		context.Response.Redirect("/swg-login.html?returnUrl=" + returnUrl);
	}
}