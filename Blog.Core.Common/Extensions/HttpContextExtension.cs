using System;
using Microsoft.AspNetCore.Http;

namespace Blog.Core.Common.Extensions;

public static class HttpContextExtension
{
    /// <summary>
    /// 这个扩展方法用于从HttpContext对象中获取ISession对象，如果获取不到则返回默认值
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static ISession GetSession(this HttpContext context) 
    {
        /*它尝试从HttpContext中获取Session属性，如果成功获取到Session对象，则将其返回；
          如果发生异常，则返回默认值。*/
        try
        {
			return context.Session;
		}
		catch (Exception)
		{
			return default;
		}
	}
}