﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using GDMS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDMS.Controllers
{
    [RequestAuthorize]
    public class TypeController : ApiController
    {

        //POST对象 (通过POST只能获取1个对象，因此POST多个数据需要使用类)
        public class TypeAjax
        {
            public string userId { get; set; }
            public string systemId { get; set; }
            public int page { get; set; }
            public int limit { get; set; }
            public string keyword { get; set; }
        }
        //返回对象
        private class Response
        {
            public int code { get; set; }
            public string count { get; set; }
            public string msg { get; set; }
            public object data { get; set; }
        }

        //获取设备列表
        [ActionName("list")]
        public HttpResponseMessage TypeList([FromBody] TypeAjax typeajax)
        {
            Db db = new Db();
            string where = "";
            if (typeajax.systemId != null) { where = where + " AND A.SYSTEM_ID = '" + typeajax.systemId + "'"; }
            if (typeajax.keyword != null && typeajax.keyword.Length != 0) { where = where + "AND ( A.NAME LIKE '%" + typeajax.keyword + "%')"; }
            string sqlnp = @"
                SELECT
                A.ID AS TYPE_ID,
                A.NAME AS TYPE_NAME,
                A.USER_ID,
                A.EDIT_DATE,
                B.ID AS SYSTEM_ID,
                B.NAME AS SYSTEM_NAME
                FROM
                GDMS_TYPE A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + typeajax.userId + "') " + where;

            int limit1 = (typeajax.page - 1) * typeajax.limit + 1;
            int limit2 = typeajax.page * typeajax.limit;
            string sql = "SELECT * FROM(SELECT p1.*,ROWNUM rn FROM(" + sqlnp + ")p1)WHERE rn BETWEEN " + limit1 + " AND " + limit2;
            var ds = db.QueryT(sql);
            Response res = new Response();
            ArrayList data = new ArrayList();
            foreach (DataRow col in ds.Rows)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    { "TYPE_ID", col["TYPE_ID"].ToString() },
                    { "TYPE_NAME", col["TYPE_NAME"].ToString() },
                    { "USER_ID", col["USER_ID"].ToString() },
                    { "EDIT_DATE", col["EDIT_DATE"].ToString() },
                    { "SYSTEM_ID", col["SYSTEM_ID"].ToString() },
                    { "SYSTEM_NAME", col["SYSTEM_NAME"].ToString() },
                };

                data.Add(dict);
            }

            string sql2 = @"
                SELECT
                COUNT(*) AS COUNT
                FROM
                GDMS_TYPE A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + typeajax.userId + "') " + where;
            var ds2 = db.QueryT(sql2);
            foreach (DataRow col in ds2.Rows)
            {
                res.count = col["count"].ToString();
            }

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //获取select
        [ActionName("select")]
        public HttpResponseMessage DeviceSelect([FromBody] TypeAjax typeajax)
        {
            Db db = new Db();
            Response res = new Response();
            Dictionary<string, object> data = new Dictionary<string, object>();

            //查询系统select
            string sql1 = @"
                SELECT
                A.SYSTEM_ID AS SYSTEM_ID,
                B.NAME AS SYSTEM_NAME
                FROM
                GDMS_USER_SYSTEM A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.USER_ID = '" + typeajax.userId + "'";
            var ds1 = db.QueryT(sql1);
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            foreach (DataRow col in ds1.Rows)
            {
                dict1.Add(col["SYSTEM_ID"].ToString(), col["SYSTEM_NAME"].ToString());
            }
            data.Add("system", dict1);

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //删除项目
        [ActionName("del")]
        public HttpResponseMessage TypeDel([FromBody] String ajaxData)
        {
            Db db = new Db();
            JArray idArr = (JArray)JsonConvert.DeserializeObject(ajaxData);
            string sqlin = "";
            foreach (var siteId in idArr)
            {
                sqlin = sqlin + siteId + ",";
            }
            sqlin = sqlin.Substring(0, sqlin.Length - 1);
            string sql = "DELETE FROM GDMS_TYPE WHERE ID IN (" + sqlin + ")";

            var rows = db.ExecuteSql(sql);
            Response res = new Response();

            res.code = 0;
            res.msg = "操作成功，删除了" + rows + "个类型";
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //添加项目
        [ActionName("add")]
        public HttpResponseMessage TypeAdd([FromBody] String ajaxData)
        {
            JObject formData = (JObject)JsonConvert.DeserializeObject(ajaxData);
            Db db = new Db();
            string sql = @"INSERT INTO GDMS_TYPE(ID,SYSTEM_ID,NAME,USER_ID,EDIT_DATE) VALUES (
                GDMS_TYPE_SEQ.nextVal, 
                '" + (String)formData["system"] + @"',
                '" + (String)formData["name"] + @"', 
                '" + (String)formData["userId"] + @"',
                SYSDATE
                )";
            var rows = db.ExecuteSql(sql);


            Response res = new Response();
            res.code = 0;
            res.msg = "添加成功" + rows;
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //修改项目
        [ActionName("edit")]
        public HttpResponseMessage TypeEdit([FromBody] String ajaxData)
        {
            JObject formData = (JObject)JsonConvert.DeserializeObject(ajaxData);
            Db db = new Db();
            string sql = @"UPDATE GDMS_TYPE SET 
                SYSTEM_ID = '" + (String)formData["system"] + @"',
                NAME = '" + (String)formData["name"] + @"',
                USER_ID = '" + (String)formData["userId"] + @"',
                EDIT_DATE = SYSDATE
                WHERE ID = '" + (String)formData["typeId"] + "'";

            var rows = db.ExecuteSql(sql);

            Response res = new Response();
            res.code = 0;
            res.msg = "更新成功" + rows;
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }



    }
}
