﻿using CYQ.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Web;

namespace Taurus.Core
{
    /// <summary>
    /// 权限检测模块
    /// </summary>
    public class UrlRewrite : IHttpModule
    {
        public void Dispose()
        {

        }
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
            context.Error += context_Error;
        }

        void context_Error(object sender, EventArgs e)
        {
            if (IsEndWithPage(HttpContext.Current.Request.Url))
            {
                Log.WriteLogToTxt(HttpContext.Current.Error);
            }
        }

        HttpContext context;
        void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            context = app.Context;
            ReplaceOutput();
            InvokeClass();
        }



        #region 替换输出，仅对子目录部署时有效
        void ReplaceOutput()
        {
            string ui = AppConfig.GetApp("UI", "").ToLower();
            string url = context.Request.Url.LocalPath.ToLower();
            if (ui != "" && url.StartsWith(ui) && IsEndWithPage(context.Request.Url, false))
            {
                //如果项目需要部署成子应用程序，则开启，否则不需要开启（可注释掉下面一行代码）
                context.Response.Filter = new HttpResponseFilter(context.Response.Filter);
            }
        }
        #endregion

        #region 逻辑反射调用Controlls的方法
        private void InvokeClass()
        {
            string localPath = context.Request.Url.LocalPath;
            Type t = null;
            if (localPath.IndexOf(".") == -1) // 处理Mvc请求
            {
                //ViewController是由页面的前两个路径决定了。
                string[] items = localPath.Trim('/').Split('/');
                string className = items[0];
                if (RouteConfig.RouteMode == 2)
                {
                    className = items.Length > 1 ? items[1] : "";
                }
                t = InvokeLogic.GetType(className);
                if (t == null)
                {
                    WriteError("Can't find the controller!");
                }
                try
                {
                    object o = Activator.CreateInstance(t);//实例化
                    t.GetMethod("ProcessRequest").Invoke(o, new object[] { context });
                }
                catch (ThreadAbortException e)
                {
                }
                catch (Exception err)
                {
                    WriteError(err.Message);
                }
            }
        }
        private void WriteError(string tip)
        {
            context.Response.Write(tip);
            context.Response.End();
        }
        #endregion

        #region 共用类
        private bool IsEndWithPage(Uri uri)
        {
            return IsEndWithPage(uri, true);
        }
        private bool IsEndWithPage(Uri uri, bool ashx)
        {
            string localPath = uri.LocalPath.ToLower();
            return localPath.EndsWith(".html") || localPath.EndsWith(".aspx") || (ashx && localPath.EndsWith(".ashx"));
        }
        #endregion

    }
}
